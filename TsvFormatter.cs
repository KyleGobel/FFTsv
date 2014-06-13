using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TsvFormatter
{
    /// <summary>
    /// Custom Order Attribute to specify we are serializing this, and what order it should be in
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class OrderAttribute : Attribute
    {
        private readonly int _order; 
        public int Order { get { return _order; }}
        public OrderAttribute(int order)
        {
            _order = order;
        }
    }


    /// <summary>
    /// Static config class containing info about how we should serialize
    /// </summary>
    public static class TsvConfig
    {
        static TsvConfig()
        {
            Delimiter = "\t";
            LineEnding = Environment.NewLine;
            DateTimeSerializeFn = dt => dt.ToString("O");
            ReplaceDelimiterInValuesWith = " ";
        }
        public static string Delimiter { get; set; }
        public static string LineEnding { get; set; }
        public static string ReplaceDelimiterInValuesWith { get; set; }
        public static Func<DateTime, string> DateTimeSerializeFn { get; set; }   
    }
    public class TsvFormatter
    {

        /// <summary>
        /// Standard issue memoize function
        /// </summary>
        private static Func<TIn, TOut> Memoize<TIn, TOut>(Func<TIn, TOut> func)
        {
            var cache = new Dictionary<TIn, TOut>();
            return (input =>
            {
                TOut result;
                if (cache.TryGetValue(input, out result)) return result;
                return cache[input] = func(input);
            });
        }


        public static readonly Func<Type, List<string>>
            GetSerializablePropertiesInOrder = Memoize<Type, List<string>>(
            t => t.GetProperties()
                .Where(x => HasOrderAttribute(x))
                .OrderBy(x => GetOrder(x))
                .Select(x => x.Name)
                .ToList()),
            GetHeaderColumns = Memoize<Type, List<string>>(GetTsvHeaderValues);

        public static readonly Func<Type, string>
            GetHeaderRow = Memoize<Type, string>(x => MakeTsvRow(GetTsvHeaderValues(x)));

        private static readonly Func<PropertyInfo, int> GetOrder =
            Memoize<PropertyInfo, int>(t =>
            {
                if (HasOrderAttribute(t))
                {
                    var orderAttr =
                        t.GetCustomAttributes(false).Single(x => x.GetType().Name == "OrderAttribute") as OrderAttribute;

                    return orderAttr.Order;
                }
                return -1;
            });

        private static readonly Func<PropertyInfo, bool> HasOrderAttribute =
            Memoize<PropertyInfo, bool>(t => t.GetCustomAttributes(false).Any(a => a.GetType().Name == "OrderAttribute")); 
        public static string MakeTsvRow(IEnumerable<string> values)
        {
            return string.Concat(
                string.Join(TsvConfig.Delimiter, values), TsvConfig.LineEnding);
        }
        private static List<string> GetTsvHeaderValues(Type t)
        {
            var names = new List<string>();
          
            foreach(var prop in t.GetProperties().Where(p => HasOrderAttribute(p)).OrderBy(x => GetOrder(x)))
            {
                var dispNameAttr = prop.GetCustomAttributes(false)
                    .FirstOrDefault(x => x.GetType().Name == "DisplayNameAttribute");

                if (dispNameAttr is DisplayNameAttribute)
                    names.Add((dispNameAttr as DisplayNameAttribute).DisplayName);
                else
                {
                    names.Add(prop.Name);
                }
            }
            return names;
        }
        public static string GetTsvRow<T>(List<string> properties, T obj)
        {
            var type = typeof (T);

            var tsvRow = string.Join(TsvConfig.Delimiter,
                properties.Select(x => SerializeToString(type.GetProperty(x).GetValue(obj))));
            return string.Concat(tsvRow, TsvConfig.LineEnding);
        }

        private static string SerializeToString(object item)
        {
            if (item is DateTime)
            {
                return TsvConfig.DateTimeSerializeFn((DateTime) item);
            }
            return item.ToString().Replace(TsvConfig.Delimiter, TsvConfig.ReplaceDelimiterInValuesWith);
        }
    }

    public static class TsvExtensions
    {
        public static string ToTsvDataRow<T>(this T objToSerialize) where T : class
        {
            var properties = TsvFormatter.GetSerializablePropertiesInOrder(typeof (T));

            return TsvFormatter.GetTsvRow(properties, objToSerialize);
        }

        public static string ToTsvHeaderRow<T>(this T objToSerialize) where T : class
        {
            return TsvFormatter.GetHeaderRow(typeof (T));
        }

        public static string ToTsv<T>(this IEnumerable<T> collectionToSerialize, bool includeHeaders = true) where T : class
        {
            var data = string.Join("", collectionToSerialize.Select(x => x.ToTsvDataRow()));
            return includeHeaders ? string.Concat(TsvFormatter.GetHeaderRow(typeof (T)), data) : data;
        }
    }
}
