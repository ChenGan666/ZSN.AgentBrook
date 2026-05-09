using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LitJson
{
  public class JsonWriter
  {
    private static NumberFormatInfo number_format = NumberFormatInfo.InvariantInfo;
    private WriterContext context;
    private Stack<WriterContext> ctx_stack;
    private bool has_reached_end;
    private char[] hex_seq;
    private int indentation;
    private int indent_value;
    private StringBuilder inst_string_builder;
    private bool pretty_print;
    private bool validate;
    private TextWriter writer;

    public int IndentValue
    {
      get
      {
        return this.indent_value;
      }
      set
      {
        this.indentation = this.indentation / this.indent_value * value;
        this.indent_value = value;
      }
    }

    public bool PrettyPrint
    {
      get
      {
        return this.pretty_print;
      }
      set
      {
        this.pretty_print = value;
      }
    }

    public TextWriter TextWriter
    {
      get
      {
        return this.writer;
      }
    }

    public bool Validate
    {
      get
      {
        return this.validate;
      }
      set
      {
        this.validate = value;
      }
    }

    public JsonWriter()
    {
      this.inst_string_builder = new StringBuilder();
      this.writer = (TextWriter) new StringWriter(this.inst_string_builder);
      this.Init();
    }

    public JsonWriter(StringBuilder sb)
      : this((TextWriter) new StringWriter(sb))
    {
    }

    public JsonWriter(TextWriter writer)
    {
      if (writer == null)
        throw new ArgumentNullException(nameof (writer));
      this.writer = writer;
      this.Init();
    }

    private void DoValidation(Condition cond)
    {
      if (!this.context.ExpectingValue)
        ++this.context.Count;
      if (!this.validate)
        return;
      if (this.has_reached_end)
        throw new JsonException("A complete JSON symbol has already been written");
      switch (cond)
      {
        case Condition.InArray:
          if (this.context.InArray)
            break;
          throw new JsonException("Can't close an array here");
        case Condition.InObject:
          if (this.context.InObject && !this.context.ExpectingValue)
            break;
          throw new JsonException("Can't close an object here");
        case Condition.NotAProperty:
          if (!this.context.InObject || this.context.ExpectingValue)
            break;
          throw new JsonException("Expected a property");
        case Condition.Property:
          if (this.context.InObject && !this.context.ExpectingValue)
            break;
          throw new JsonException("Can't add a property here");
        case Condition.Value:
          if (this.context.InArray || this.context.InObject && this.context.ExpectingValue)
            break;
          throw new JsonException("Can't add a value here");
      }
    }

    private void Init()
    {
      this.has_reached_end = false;
      this.hex_seq = new char[4];
      this.indentation = 0;
      this.indent_value = 4;
      this.pretty_print = false;
      this.validate = true;
      this.ctx_stack = new Stack<WriterContext>();
      this.context = new WriterContext();
      this.ctx_stack.Push(this.context);
    }

    private static void IntToHex(int n, char[] hex)
    {
      for (int index = 0; index < 4; ++index)
      {
        int num = n % 16;
        hex[3 - index] = num >= 10 ? (char) (65 + (num - 10)) : (char) (48 + num);
        n >>= 4;
      }
    }

    private void Indent()
    {
      if (!this.pretty_print)
        return;
      this.indentation += this.indent_value;
    }

    private void Put(string str)
    {
      if (this.pretty_print && !this.context.ExpectingValue)
      {
        for (int index = 0; index < this.indentation; ++index)
          this.writer.Write(' ');
      }
      this.writer.Write(str);
    }

    private void PutNewline()
    {
      this.PutNewline(true);
    }

    private void PutNewline(bool add_comma)
    {
      if (add_comma && !this.context.ExpectingValue && this.context.Count > 1)
        this.writer.Write(',');
      if (!this.pretty_print || this.context.ExpectingValue)
        return;
      this.writer.Write('\n');
    }

    private void PutString(string str, bool toHex = true)
    {
      this.Put(string.Empty);
      this.writer.Write('"');
      int length = str.Length;
      for (int index = 0; index < length; ++index)
      {
        switch (str[index])
        {
          case '\b':
            this.writer.Write("\\b");
            break;
          case '\t':
            this.writer.Write("\\t");
            break;
          case '\n':
            this.writer.Write("\\n");
            break;
          case '\f':
            this.writer.Write("\\f");
            break;
          case '\r':
            this.writer.Write("\\r");
            break;
          case '"':
          case '\\':
            this.writer.Write('\\');
            this.writer.Write(str[index]);
            break;
          default:
            if (str[index] >= ' ' && str[index] <= '~')
            {
              this.writer.Write(str[index]);
              break;
            }
            if (toHex)
            {
              JsonWriter.IntToHex((int) str[index], this.hex_seq);
              this.writer.Write("\\u");
              this.writer.Write(this.hex_seq);
              break;
            }
            this.writer.Write(str[index]);
            break;
        }
      }
      this.writer.Write('"');
    }

    private void Unindent()
    {
      if (!this.pretty_print)
        return;
      this.indentation -= this.indent_value;
    }

    public override string ToString()
    {
      if (this.inst_string_builder == null)
        return string.Empty;
      return this.inst_string_builder.ToString();
    }

    public void Reset()
    {
      this.has_reached_end = false;
      this.ctx_stack.Clear();
      this.context = new WriterContext();
      this.ctx_stack.Push(this.context);
      if (this.inst_string_builder == null)
        return;
      this.inst_string_builder.Remove(0, this.inst_string_builder.Length);
    }

    public void Write(bool boolean)
    {
      this.DoValidation(Condition.Value);
      this.PutNewline();
      this.Put(boolean ? "true" : "false");
      this.context.ExpectingValue = false;
    }

    public void Write(Decimal number)
    {
      this.DoValidation(Condition.Value);
      this.PutNewline();
      this.Put(Convert.ToString(number, (IFormatProvider) JsonWriter.number_format));
      this.context.ExpectingValue = false;
    }

    public void Write(double number)
    {
      this.DoValidation(Condition.Value);
      this.PutNewline();
      string str = Convert.ToString(number, (IFormatProvider) JsonWriter.number_format);
      this.Put(str);
      if (str.IndexOf('.') == -1 && str.IndexOf('E') == -1)
        this.writer.Write(".0");
      this.context.ExpectingValue = false;
    }

    public void Write(int number)
    {
      this.DoValidation(Condition.Value);
      this.PutNewline();
      this.Put(Convert.ToString(number, (IFormatProvider) JsonWriter.number_format));
      this.context.ExpectingValue = false;
    }

    public void Write(long number)
    {
      this.DoValidation(Condition.Value);
      this.PutNewline();
      this.Put(Convert.ToString(number, (IFormatProvider) JsonWriter.number_format));
      this.context.ExpectingValue = false;
    }

    public void Write(string str, bool toHex = true)
    {
      this.DoValidation(Condition.Value);
      this.PutNewline();
      if (str == null)
        this.Put("null");
      else
        this.PutString(str, toHex);
      this.context.ExpectingValue = false;
    }

    public void Write(ulong number)
    {
      this.DoValidation(Condition.Value);
      this.PutNewline();
      this.Put(Convert.ToString(number, (IFormatProvider) JsonWriter.number_format));
      this.context.ExpectingValue = false;
    }

    public void WriteArrayEnd()
    {
      this.DoValidation(Condition.InArray);
      this.PutNewline(false);
      this.ctx_stack.Pop();
      if (this.ctx_stack.Count == 1)
      {
        this.has_reached_end = true;
      }
      else
      {
        this.context = this.ctx_stack.Peek();
        this.context.ExpectingValue = false;
      }
      this.Unindent();
      this.Put("]");
    }

    public void WriteArrayStart()
    {
      this.DoValidation(Condition.NotAProperty);
      this.PutNewline();
      this.Put("[");
      this.context = new WriterContext();
      this.context.InArray = true;
      this.ctx_stack.Push(this.context);
      this.Indent();
    }

    public void WriteObjectEnd()
    {
      this.DoValidation(Condition.InObject);
      this.PutNewline(false);
      this.ctx_stack.Pop();
      if (this.ctx_stack.Count == 1)
      {
        this.has_reached_end = true;
      }
      else
      {
        this.context = this.ctx_stack.Peek();
        this.context.ExpectingValue = false;
      }
      this.Unindent();
      this.Put("}");
    }

    public void WriteObjectStart()
    {
      this.DoValidation(Condition.NotAProperty);
      this.PutNewline();
      this.Put("{");
      this.context = new WriterContext();
      this.context.InObject = true;
      this.ctx_stack.Push(this.context);
      this.Indent();
    }

    public void WritePropertyName(string property_name)
    {
      this.DoValidation(Condition.Property);
      this.PutNewline();
      this.PutString(property_name, true);
      if (this.pretty_print)
      {
        if (property_name.Length > this.context.Padding)
          this.context.Padding = property_name.Length;
        for (int index = this.context.Padding - property_name.Length; index >= 0; --index)
          this.writer.Write(' ');
        this.writer.Write(": ");
      }
      else
        this.writer.Write(':');
      this.context.ExpectingValue = true;
    }
  }
}
