using System;
using System.Collections;
using System.Collections.Specialized;

namespace LitJson
{
  public class JsonMockWrapper : IJsonWrapper, IList, ICollection, IEnumerable, IOrderedDictionary, IDictionary
  {
    public bool IsArray
    {
      get
      {
        return false;
      }
    }

    public bool IsBoolean
    {
      get
      {
        return false;
      }
    }

    public bool IsDouble
    {
      get
      {
        return false;
      }
    }

    public bool IsInt
    {
      get
      {
        return false;
      }
    }

    public bool IsLong
    {
      get
      {
        return false;
      }
    }

    public bool IsObject
    {
      get
      {
        return false;
      }
    }

    public bool IsString
    {
      get
      {
        return false;
      }
    }

    public bool GetBoolean()
    {
      return false;
    }

    public double GetDouble()
    {
      return 0.0;
    }

    public int GetInt()
    {
      return 0;
    }

    public JsonType GetJsonType()
    {
      return JsonType.None;
    }

    public long GetLong()
    {
      return 0;
    }

    public string GetString()
    {
      return "";
    }

    public void SetBoolean(bool val)
    {
    }

    public void SetDouble(double val)
    {
    }

    public void SetInt(int val)
    {
    }

    public void SetJsonType(JsonType type)
    {
    }

    public void SetLong(long val)
    {
    }

    public void SetString(string val)
    {
    }

    public string ToJson()
    {
      return "";
    }

    public void ToJson(JsonWriter writer)
    {
    }

    bool IList.IsFixedSize
    {
      get
      {
        return true;
      }
    }

    bool IList.IsReadOnly
    {
      get
      {
        return true;
      }
    }

    object IList.this[int index]
    {
      get
      {
        return (object) null;
      }
      set
      {
      }
    }

    int IList.Add(object value)
    {
      return 0;
    }

    void IList.Clear()
    {
    }

    bool IList.Contains(object value)
    {
      return false;
    }

    int IList.IndexOf(object value)
    {
      return -1;
    }

    void IList.Insert(int i, object v)
    {
    }

    void IList.Remove(object value)
    {
    }

    void IList.RemoveAt(int index)
    {
    }

    int ICollection.Count
    {
      get
      {
        return 0;
      }
    }

    bool ICollection.IsSynchronized
    {
      get
      {
        return false;
      }
    }

    object ICollection.SyncRoot
    {
      get
      {
        return (object) null;
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) null;
    }

    bool IDictionary.IsFixedSize
    {
      get
      {
        return true;
      }
    }

    bool IDictionary.IsReadOnly
    {
      get
      {
        return true;
      }
    }

    ICollection IDictionary.Keys
    {
      get
      {
        return (ICollection) null;
      }
    }

    ICollection IDictionary.Values
    {
      get
      {
        return (ICollection) null;
      }
    }

    object IDictionary.this[object key]
    {
      get
      {
        return (object) null;
      }
      set
      {
      }
    }

    void IDictionary.Add(object k, object v)
    {
    }

    void IDictionary.Clear()
    {
    }

    bool IDictionary.Contains(object key)
    {
      return false;
    }

    void IDictionary.Remove(object key)
    {
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
      return (IDictionaryEnumerator) null;
    }

    object IOrderedDictionary.this[int idx]
    {
      get
      {
        return (object) null;
      }
      set
      {
      }
    }

    IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
    {
      return (IDictionaryEnumerator) null;
    }

    void IOrderedDictionary.Insert(int i, object k, object v)
    {
    }

    void IOrderedDictionary.RemoveAt(int i)
    {
    }
  }
}
