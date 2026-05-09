using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace LitJson
{
  public class JsonData : IJsonWrapper, IList, ICollection, IEnumerable, IOrderedDictionary, IDictionary, IEquatable<JsonData>
  {
    private IList<JsonData> inst_array;
    private bool inst_boolean;
    private double inst_double;
    private int inst_int;
    private long inst_long;
    private IDictionary<string, JsonData> inst_object;
    private string inst_string;
    private string json;
    private JsonType type;
    private IList<KeyValuePair<string, JsonData>> object_list;

    public int Count
    {
      get
      {
        return this.EnsureCollection().Count;
      }
    }

    public bool IsArray
    {
      get
      {
        return this.type == JsonType.Array;
      }
    }

    public bool IsBoolean
    {
      get
      {
        return this.type == JsonType.Boolean;
      }
    }

    public bool IsDouble
    {
      get
      {
        return this.type == JsonType.Double;
      }
    }

    public bool IsInt
    {
      get
      {
        return this.type == JsonType.Int;
      }
    }

    public bool IsLong
    {
      get
      {
        return this.type == JsonType.Long;
      }
    }

    public bool IsObject
    {
      get
      {
        return this.type == JsonType.Object;
      }
    }

    public bool IsString
    {
      get
      {
        return this.type == JsonType.String;
      }
    }

    public ICollection<string> Keys
    {
      get
      {
        this.EnsureDictionary();
        return this.inst_object.Keys;
      }
    }

    int ICollection.Count
    {
      get
      {
        return this.Count;
      }
    }

    bool ICollection.IsSynchronized
    {
      get
      {
        return this.EnsureCollection().IsSynchronized;
      }
    }

    object ICollection.SyncRoot
    {
      get
      {
        return this.EnsureCollection().SyncRoot;
      }
    }

    bool IDictionary.IsFixedSize
    {
      get
      {
        return this.EnsureDictionary().IsFixedSize;
      }
    }

    bool IDictionary.IsReadOnly
    {
      get
      {
        return this.EnsureDictionary().IsReadOnly;
      }
    }

    ICollection IDictionary.Keys
    {
      get
      {
        this.EnsureDictionary();
        IList<string> stringList = (IList<string>) new List<string>();
        foreach (KeyValuePair<string, JsonData> keyValuePair in (IEnumerable<KeyValuePair<string, JsonData>>) this.object_list)
          stringList.Add(keyValuePair.Key);
        return (ICollection) stringList;
      }
    }

    ICollection IDictionary.Values
    {
      get
      {
        this.EnsureDictionary();
        IList<JsonData> jsonDataList = (IList<JsonData>) new List<JsonData>();
        foreach (KeyValuePair<string, JsonData> keyValuePair in (IEnumerable<KeyValuePair<string, JsonData>>) this.object_list)
          jsonDataList.Add(keyValuePair.Value);
        return (ICollection) jsonDataList;
      }
    }

    bool IJsonWrapper.IsArray
    {
      get
      {
        return this.IsArray;
      }
    }

    bool IJsonWrapper.IsBoolean
    {
      get
      {
        return this.IsBoolean;
      }
    }

    bool IJsonWrapper.IsDouble
    {
      get
      {
        return this.IsDouble;
      }
    }

    bool IJsonWrapper.IsInt
    {
      get
      {
        return this.IsInt;
      }
    }

    bool IJsonWrapper.IsLong
    {
      get
      {
        return this.IsLong;
      }
    }

    bool IJsonWrapper.IsObject
    {
      get
      {
        return this.IsObject;
      }
    }

    bool IJsonWrapper.IsString
    {
      get
      {
        return this.IsString;
      }
    }

    bool IList.IsFixedSize
    {
      get
      {
        return this.EnsureList().IsFixedSize;
      }
    }

    bool IList.IsReadOnly
    {
      get
      {
        return this.EnsureList().IsReadOnly;
      }
    }

    object IDictionary.this[object key]
    {
      get
      {
        return this.EnsureDictionary()[key];
      }
      set
      {
        if (!(key is string))
          throw new ArgumentException("The key has to be a string");
        JsonData jsonData = this.ToJsonData(value);
        this[(string) key] = jsonData;
      }
    }

    object IOrderedDictionary.this[int idx]
    {
      get
      {
        this.EnsureDictionary();
        return (object) this.object_list[idx].Value;
      }
      set
      {
        this.EnsureDictionary();
        JsonData jsonData = this.ToJsonData(value);
        KeyValuePair<string, JsonData> keyValuePair1 = this.object_list[idx];
        this.inst_object[keyValuePair1.Key] = jsonData;
        KeyValuePair<string, JsonData> keyValuePair2 = new KeyValuePair<string, JsonData>(keyValuePair1.Key, jsonData);
        this.object_list[idx] = keyValuePair2;
      }
    }

    object IList.this[int index]
    {
      get
      {
        return this.EnsureList()[index];
      }
      set
      {
        this.EnsureList();
        JsonData jsonData = this.ToJsonData(value);
        this[index] = jsonData;
      }
    }

    public JsonData this[string prop_name]
    {
      get
      {
        this.EnsureDictionary();
        return this.inst_object[prop_name];
      }
      set
      {
        this.EnsureDictionary();
        KeyValuePair<string, JsonData> keyValuePair = new KeyValuePair<string, JsonData>(prop_name, value);
        if (this.inst_object.ContainsKey(prop_name))
        {
          for (int index = 0; index < this.object_list.Count; ++index)
          {
            if (this.object_list[index].Key == prop_name)
            {
              this.object_list[index] = keyValuePair;
              break;
            }
          }
        }
        else
          this.object_list.Add(keyValuePair);
        this.inst_object[prop_name] = value;
        this.json = (string) null;
      }
    }

    public JsonData this[int index]
    {
      get
      {
        this.EnsureCollection();
        if (this.type == JsonType.Array)
          return this.inst_array[index];
        return this.object_list[index].Value;
      }
      set
      {
        this.EnsureCollection();
        if (this.type == JsonType.Array)
        {
          this.inst_array[index] = value;
        }
        else
        {
          KeyValuePair<string, JsonData> keyValuePair1 = this.object_list[index];
          KeyValuePair<string, JsonData> keyValuePair2 = new KeyValuePair<string, JsonData>(keyValuePair1.Key, value);
          this.object_list[index] = keyValuePair2;
          this.inst_object[keyValuePair1.Key] = value;
        }
        this.json = (string) null;
      }
    }

    public JsonData()
    {
    }

    public JsonData(bool boolean)
    {
      this.type = JsonType.Boolean;
      this.inst_boolean = boolean;
    }

    public JsonData(double number)
    {
      this.type = JsonType.Double;
      this.inst_double = number;
    }

    public JsonData(int number)
    {
      this.type = JsonType.Int;
      this.inst_int = number;
    }

    public JsonData(long number)
    {
      this.type = JsonType.Long;
      this.inst_long = number;
    }

    public JsonData(object obj)
    {
      if (obj is bool)
      {
        this.type = JsonType.Boolean;
        this.inst_boolean = (bool) obj;
      }
      else if (obj is double)
      {
        this.type = JsonType.Double;
        this.inst_double = (double) obj;
      }
      else if (obj is int)
      {
        this.type = JsonType.Int;
        this.inst_int = (int) obj;
      }
      else if (obj is long)
      {
        this.type = JsonType.Long;
        this.inst_long = (long) obj;
      }
      else
      {
        if (!(obj is string))
          throw new ArgumentException("Unable to wrap the given object with JsonData");
        this.type = JsonType.String;
        this.inst_string = (string) obj;
      }
    }

    public JsonData(string str)
    {
      this.type = JsonType.String;
      this.inst_string = str;
    }

    public static implicit operator JsonData(bool data)
    {
      return new JsonData(data);
    }

    public static implicit operator JsonData(double data)
    {
      return new JsonData(data);
    }

    public static implicit operator JsonData(int data)
    {
      return new JsonData(data);
    }

    public static implicit operator JsonData(long data)
    {
      return new JsonData(data);
    }

    public static implicit operator JsonData(string data)
    {
      return new JsonData(data);
    }

    public static explicit operator bool(JsonData data)
    {
      if (data.type != JsonType.Boolean)
        throw new InvalidCastException("Instance of JsonData doesn't hold a double");
      return data.inst_boolean;
    }

    public static explicit operator double(JsonData data)
    {
      if (data.type != JsonType.Double)
        throw new InvalidCastException("Instance of JsonData doesn't hold a double");
      return data.inst_double;
    }

    public static explicit operator int(JsonData data)
    {
      if (data.type != JsonType.Int)
        throw new InvalidCastException("Instance of JsonData doesn't hold an int");
      return data.inst_int;
    }

    public static explicit operator long(JsonData data)
    {
      if (data.type != JsonType.Long)
        throw new InvalidCastException("Instance of JsonData doesn't hold an int");
      return data.inst_long;
    }

    public static explicit operator string(JsonData data)
    {
      if (data.type != JsonType.String)
        throw new InvalidCastException("Instance of JsonData doesn't hold a string");
      return data.inst_string;
    }

    void ICollection.CopyTo(Array array, int index)
    {
      this.EnsureCollection().CopyTo(array, index);
    }

    void IDictionary.Add(object key, object value)
    {
      JsonData jsonData = this.ToJsonData(value);
      this.EnsureDictionary().Add(key, (object) jsonData);
      this.object_list.Add(new KeyValuePair<string, JsonData>((string) key, jsonData));
      this.json = (string) null;
    }

    void IDictionary.Clear()
    {
      this.EnsureDictionary().Clear();
      this.object_list.Clear();
      this.json = (string) null;
    }

    bool IDictionary.Contains(object key)
    {
      return this.EnsureDictionary().Contains(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
      return ((IOrderedDictionary) this).GetEnumerator();
    }

    void IDictionary.Remove(object key)
    {
      this.EnsureDictionary().Remove(key);
      for (int index = 0; index < this.object_list.Count; ++index)
      {
        if (this.object_list[index].Key == (string) key)
        {
          this.object_list.RemoveAt(index);
          break;
        }
      }
      this.json = (string) null;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.EnsureCollection().GetEnumerator();
    }

    bool IJsonWrapper.GetBoolean()
    {
      if (this.type != JsonType.Boolean)
        throw new InvalidOperationException("JsonData instance doesn't hold a boolean");
      return this.inst_boolean;
    }

    double IJsonWrapper.GetDouble()
    {
      if (this.type != JsonType.Double)
        throw new InvalidOperationException("JsonData instance doesn't hold a double");
      return this.inst_double;
    }

    int IJsonWrapper.GetInt()
    {
      if (this.type != JsonType.Int)
        throw new InvalidOperationException("JsonData instance doesn't hold an int");
      return this.inst_int;
    }

    long IJsonWrapper.GetLong()
    {
      if (this.type != JsonType.Long)
        throw new InvalidOperationException("JsonData instance doesn't hold a long");
      return this.inst_long;
    }

    string IJsonWrapper.GetString()
    {
      if (this.type != JsonType.String)
        throw new InvalidOperationException("JsonData instance doesn't hold a string");
      return this.inst_string;
    }

    void IJsonWrapper.SetBoolean(bool val)
    {
      this.type = JsonType.Boolean;
      this.inst_boolean = val;
      this.json = (string) null;
    }

    void IJsonWrapper.SetDouble(double val)
    {
      this.type = JsonType.Double;
      this.inst_double = val;
      this.json = (string) null;
    }

    void IJsonWrapper.SetInt(int val)
    {
      this.type = JsonType.Int;
      this.inst_int = val;
      this.json = (string) null;
    }

    void IJsonWrapper.SetLong(long val)
    {
      this.type = JsonType.Long;
      this.inst_long = val;
      this.json = (string) null;
    }

    void IJsonWrapper.SetString(string val)
    {
      this.type = JsonType.String;
      this.inst_string = val;
      this.json = (string) null;
    }

    string IJsonWrapper.ToJson()
    {
      return this.ToJson(true);
    }

    void IJsonWrapper.ToJson(JsonWriter writer)
    {
      this.ToJson(writer);
    }

    int IList.Add(object value)
    {
      return this.Add(value);
    }

    void IList.Clear()
    {
      this.EnsureList().Clear();
      this.json = (string) null;
    }

    bool IList.Contains(object value)
    {
      return this.EnsureList().Contains(value);
    }

    int IList.IndexOf(object value)
    {
      return this.EnsureList().IndexOf(value);
    }

    void IList.Insert(int index, object value)
    {
      this.EnsureList().Insert(index, value);
      this.json = (string) null;
    }

    void IList.Remove(object value)
    {
      this.EnsureList().Remove(value);
      this.json = (string) null;
    }

    void IList.RemoveAt(int index)
    {
      this.EnsureList().RemoveAt(index);
      this.json = (string) null;
    }

    IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
    {
      this.EnsureDictionary();
      return (IDictionaryEnumerator) new OrderedDictionaryEnumerator(this.object_list.GetEnumerator());
    }

    void IOrderedDictionary.Insert(int idx, object key, object value)
    {
      string key1 = (string) key;
      JsonData jsonData = this.ToJsonData(value);
      this[key1] = jsonData;
      KeyValuePair<string, JsonData> keyValuePair = new KeyValuePair<string, JsonData>(key1, jsonData);
      this.object_list.Insert(idx, keyValuePair);
    }

    void IOrderedDictionary.RemoveAt(int idx)
    {
      this.EnsureDictionary();
      this.inst_object.Remove(this.object_list[idx].Key);
      this.object_list.RemoveAt(idx);
    }

    private ICollection EnsureCollection()
    {
      if (this.type == JsonType.Array)
        return (ICollection) this.inst_array;
      if (this.type == JsonType.Object)
        return (ICollection) this.inst_object;
      throw new InvalidOperationException("The JsonData instance has to be initialized first");
    }

    private IDictionary EnsureDictionary()
    {
      if (this.type == JsonType.Object)
        return (IDictionary) this.inst_object;
      if (this.type != JsonType.None)
        throw new InvalidOperationException("Instance of JsonData is not a dictionary");
      this.type = JsonType.Object;
      this.inst_object = (IDictionary<string, JsonData>) new Dictionary<string, JsonData>();
      this.object_list = (IList<KeyValuePair<string, JsonData>>) new List<KeyValuePair<string, JsonData>>();
      return (IDictionary) this.inst_object;
    }

    private IList EnsureList()
    {
      if (this.type == JsonType.Array)
        return (IList) this.inst_array;
      if (this.type != JsonType.None)
        throw new InvalidOperationException("Instance of JsonData is not a list");
      this.type = JsonType.Array;
      this.inst_array = (IList<JsonData>) new List<JsonData>();
      return (IList) this.inst_array;
    }

    private JsonData ToJsonData(object obj)
    {
      if (obj == null)
        return (JsonData) null;
      if (obj is JsonData)
        return (JsonData) obj;
      return new JsonData(obj);
    }

    private static void WriteJson(IJsonWrapper obj, JsonWriter writer, bool toHex = true)
    {
      if (obj == null)
        writer.Write((string) null, true);
      else if (obj.IsString)
        writer.Write(obj.GetString(), toHex);
      else if (obj.IsBoolean)
        writer.Write(obj.GetBoolean());
      else if (obj.IsDouble)
        writer.Write(obj.GetDouble());
      else if (obj.IsInt)
        writer.Write(obj.GetInt());
      else if (obj.IsLong)
        writer.Write(obj.GetLong());
      else if (obj.IsArray)
      {
        writer.WriteArrayStart();
        foreach (IJsonWrapper jsonWrapper in (IEnumerable) obj)
          JsonData.WriteJson(jsonWrapper, writer, toHex);
        writer.WriteArrayEnd();
      }
      else
      {
        if (!obj.IsObject)
          return;
        writer.WriteObjectStart();
        foreach (DictionaryEntry dictionaryEntry in (IDictionary) obj)
        {
          writer.WritePropertyName((string) dictionaryEntry.Key);
          JsonData.WriteJson((IJsonWrapper) dictionaryEntry.Value, writer, toHex);
        }
        writer.WriteObjectEnd();
      }
    }

    public int Add(object value)
    {
      JsonData jsonData = this.ToJsonData(value);
      this.json = (string) null;
      return this.EnsureList().Add((object) jsonData);
    }

    public void Clear()
    {
      if (this.IsObject)
      {
        ((IDictionary) this).Clear();
      }
      else
      {
        if (!this.IsArray)
          return;
        ((IList) this).Clear();
      }
    }

    public bool ContainsKey(string key)
    {
      return this.Keys.Contains(key);
    }

    public bool Equals(JsonData x)
    {
      if (x == null || x.type != this.type)
        return false;
      switch (this.type)
      {
        case JsonType.None:
          return true;
        case JsonType.Object:
          return this.inst_object.Equals((object) x.inst_object);
        case JsonType.Array:
          return this.inst_array.Equals((object) x.inst_array);
        case JsonType.String:
          return this.inst_string.Equals(x.inst_string);
        case JsonType.Int:
          return this.inst_int.Equals(x.inst_int);
        case JsonType.Long:
          return this.inst_long.Equals(x.inst_long);
        case JsonType.Double:
          return this.inst_double.Equals(x.inst_double);
        case JsonType.Boolean:
          return this.inst_boolean.Equals(x.inst_boolean);
        default:
          return false;
      }
    }

    public JsonType GetJsonType()
    {
      return this.type;
    }

    public void SetJsonType(JsonType type)
    {
      if (this.type == type)
        return;
      switch (type)
      {
        case JsonType.Object:
          this.inst_object = (IDictionary<string, JsonData>) new Dictionary<string, JsonData>();
          this.object_list = (IList<KeyValuePair<string, JsonData>>) new List<KeyValuePair<string, JsonData>>();
          break;
        case JsonType.Array:
          this.inst_array = (IList<JsonData>) new List<JsonData>();
          break;
        case JsonType.String:
          this.inst_string = (string) null;
          break;
        case JsonType.Int:
          this.inst_int = 0;
          break;
        case JsonType.Long:
          this.inst_long = 0L;
          break;
        case JsonType.Double:
          this.inst_double = 0.0;
          break;
        case JsonType.Boolean:
          this.inst_boolean = false;
          break;
      }
      this.type = type;
    }

    public string ToJson(bool toHex = true)
    {
      if (this.json != null || this == null || !this.IsArray && this.Keys.Count == 0 || this.IsArray && this.Count == 0)
        return "{}";
      StringWriter stringWriter = new StringWriter();
      JsonData.WriteJson((IJsonWrapper) this, new JsonWriter((TextWriter) stringWriter)
      {
        Validate = false
      }, toHex);
      this.json = stringWriter.ToString();
      return this.json;
    }

    public void ToJson(JsonWriter writer)
    {
      bool validate = writer.Validate;
      writer.Validate = false;
      JsonData.WriteJson((IJsonWrapper) this, writer, true);
      writer.Validate = validate;
    }

    public override string ToString()
    {
      switch (this.type)
      {
        case JsonType.Object:
          return "JsonData object";
        case JsonType.Array:
          return "JsonData array";
        case JsonType.String:
          return this.inst_string;
        case JsonType.Int:
          return this.inst_int.ToString();
        case JsonType.Long:
          return this.inst_long.ToString();
        case JsonType.Double:
          return this.inst_double.ToString();
        case JsonType.Boolean:
          return this.inst_boolean.ToString();
        default:
          return "Uninitialized JsonData";
      }
    }
  }
}
