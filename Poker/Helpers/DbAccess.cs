using Dapper;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Configuration;
using Poker.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Poker.Helpers
{
  public interface ISavable
  {
    public Guid Id { get; set; }
    public byte[] Data
    {
      get
      {
        IFormatter formatter = new BinaryFormatter();
        using var ms = new MemoryStream();
        formatter.Serialize(ms, this);
        return ms.ToArray();
      }
    }
  }

  public class MySqlGuidTypeHandler : SqlMapper.TypeHandler<Guid>
  {
    public override void SetValue(IDbDataParameter parameter, Guid guid)
    {
      parameter.Value = guid.ToString();
    }

    public override Guid Parse(object value)
    {
      return new Guid((string)value);
    }
  }

  public class DbAccess
  {
    private static SQLiteConnection GetOpenConnection()
    {
      if (!File.Exists("Poker.db")) File.WriteAllBytes("Poker.db", new byte[0]);
      var cnn = new SQLiteConnection("DataSource=Poker.db");
      cnn.Open();
      return cnn;
    }

    private class SerializedData
    {
      public Guid Id;
      public byte[] Data;
    }

    private static IEnumerable<SerializedData> GetData<T>(SQLiteConnection cnn)
    {
      bool tryAgain = true;
      while (tryAgain)
      {
        try
        {
          return cnn.Query<SerializedData>($"select * from {typeof(T).ToString().Replace('.', '_')}");
        }
        catch (Exception e)
        {
          SQLiteCommand command = new SQLiteCommand($"create table {typeof(T).ToString().Replace('.', '_')} (Id varchar(36) not null unique, Data blob, primary key(ID))", cnn);
          command.ExecuteNonQuery();
        }
      }
      return null;
    }

    public static List<T> Load<T>()
    {
      IFormatter formatter = new BinaryFormatter();
      using var ms = new MemoryStream();
      using SQLiteConnection cnn = GetOpenConnection();
      var rawData = GetData<T>(cnn);
      return rawData.Select(rd =>
      {
        ms.Write(rd.Data, 0, rd.Data.Length);
        ms.Seek(0, SeekOrigin.Begin);
        return (T)formatter.Deserialize(ms);
      }).ToList();
    }

    public static void Save<T>(ISavable data)
    {
      using SQLiteConnection cnn = GetOpenConnection();
      cnn.Execute($"insert into {typeof(T).ToString().Replace('.', '_')} (Id, Data) values (@Id, @Data) on conflict(id) do update set Data = excluded.Data;", new { data.Id, data.Data });
    }

  }
}

//INSERT INTO phonebook(name,phonenumber) VALUES('Alice','704-555-1212')
//ON CONFLICT(name) DO UPDATE SET phonenumber = excluded.phonenumber;