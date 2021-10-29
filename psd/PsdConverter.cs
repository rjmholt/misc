using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PsdConversion
{
    public class PsdConverter
    {
        private readonly bool _usePrettyFormatting;

        public PsdConverter()
            : this(usePrettyFormatting: true)
        {
        }

        public PsdConverter(bool usePrettyFormatting)
        {
            _usePrettyFormatting = usePrettyFormatting;
        }

        public string Serialize(object o)
        {
            var writer = new StringWriter();
            var serializer = new SingleThreadedPsdConverter(_usePrettyFormatting, writer).Serialize(o);
            return writer.ToString();
        }
    }

    internal enum PsdType
    {
        Object,
        Array,
        Number,
        Bool,
        String,
        Null
    }

    internal class SingleThreadedPsdConverter
    {
        private readonly bool _usePrettyFormatting;

        private readonly TextWriter _writer;

        public SingleThreadedPsdConverter(bool usePrettyFormatting, TextWriter writer)
        {
            _usePrettyFormatting = usePrettyFormatting;
            _writer = writer;
        }

        public void Serialize(object o)
        {
            if (o == null)
            {
                _writer.Write("$null");
                return;
            }

            Type t = o.GetType();

            if (t == typeof(bool))
            {
                _writer.Write((bool)o ? "$true" : "$false");
                return;
            }

            if (t == typeof(string))
            {
                _writer.Write('\'');
                _writer.Write((string)o);
                _writer.Write('\'');
                return;
            }

            if (t == typeof(char))
            {
                _writer.Write('\'');
                _writer.Write((char)o);
                _writer.Write('\'');
                return;
            }

            if (IsNumeric(t))
            {
                _writer.Write(o.ToString());
                return;
            }

            if (typeof(IDictionary<,>).IsAssignableFrom(t.GetGenericTypeDefinition()))
            {

            }

            if (IsArraylike(o, t, out object[] arr))
            {
                _writer.Write("@(");
                int i = 0;
                for (; i < arr.Length - 1; i++)
                {
                    Serialize(arr[i]);
                    _writer.Write(',');
                }
                Serialize(arr[i]);
                _writer.Write(")");
            }
        }

        private static bool IsNumeric(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
            }

            return false;
        }

        private static bool IsArraylike(object o, Type t, out object[] arr)
        {
            if (t.IsArray)
            {
                arr = (object[])o;
                return true;
            }

            if (o is IEnumerable enumerable)
            {
                arr = enumerable.Cast<object>().ToArray();
                return true;
            }

            if (o is ICollection collection)
            {
                arr = new object[collection.Count];
                collection.CopyTo(arr, 0);
                return true;
            }

            foreach (Type iface in t.GetInterfaces())
            {
                Type genericInterface = iface.GetGenericTypeDefinition();

                if (typeof(IEnumerable<>).IsAssignableFrom(genericInterface))
                {
                    arr = Enumerable.ToArray((dynamic)o);
                    return true;
                }
            }

            arr = null;
            return false;
        }
    }
}
