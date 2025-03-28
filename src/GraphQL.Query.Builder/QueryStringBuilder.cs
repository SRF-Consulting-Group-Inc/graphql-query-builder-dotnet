using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace GraphQL.Query.Builder;

/// <summary>The GraphQL query builder class.</summary>
public class QueryStringBuilder : IQueryStringBuilder
{
    /// <summary>The property name formatter.</summary>
    protected readonly Func<PropertyInfo, string>? formatter;

    /// <summary>The query string builder.</summary>
    public StringBuilder QueryString { get; } = new();

    /// <summary>Initializes a new instance of the <see cref="QueryStringBuilder" /> class.</summary>
    public QueryStringBuilder() { }

    /// <summary>Initializes a new instance of the <see cref="QueryStringBuilder" /> class.</summary>
    /// <param name="formatter">The property name formatter</param>
    public QueryStringBuilder(Func<PropertyInfo, string> formatter)
    {
        this.formatter = formatter;
    }

    /// <summary>Builds the query.</summary>
    /// <param name="query">The query.</param>
    /// <returns>The GraphQL query as string, without outer enclosing block.</returns>
    public string Build<TSource>(IQuery<TSource> query)
    {
        if (!string.IsNullOrWhiteSpace(query.AliasName))
        {
            this.QueryString.Append($"{query.AliasName}:");
        }

        this.QueryString.Append(query.Name);

        if (query.Arguments.Count > 0)
        {
            this.QueryString.Append("(");
            this.AddParams(query);
            this.QueryString.Append(")");
        }

        if (query.SelectList.Count > 0)
        {
            this.QueryString.Append("{");
            this.AddFields(query);
            this.QueryString.Append("}");
        }

        return this.QueryString.ToString();
    }

    /// <summary>Clears the string builder.</summary>
    public void Clear()
    {
        this.QueryString.Clear();
    }

    /// <summary>
    /// Formats query param.
    /// 
    /// Returns:
    ///   <list type="bullet">
    ///     <item>
    ///       <term>null</term>
    ///       <description><c>null</c></description>
    ///     </item>
    ///     <item>
    ///       <term>String</term>
    ///       <description><c>"foo"</c></description>
    ///     </item>
    ///     <item>
    ///       <term>Number</term>
    ///       <description><c>10</c></description>
    ///     </item>
    ///     <item>
    ///       <term>Boolean</term>
    ///       <description><c>true</c> or <c>false</c></description>
    ///     </item>
    ///     <item>
    ///       <term>Enum</term>
    ///       <description><c>EnumValue</c></description>
    ///     </item>
    ///     <item>
    ///       <term>DateTime</term>
    ///       <description><c>"2024-06-15T13:45:30.0000000Z"</c></description>
    ///     </item>
    ///     <item>
    ///       <term>Key value pair</term>
    ///       <description><c>foo:"bar"</c> or <c>foo:10</c> ...</description>
    ///     </item>
    ///     <item>
    ///       <term>List</term>
    ///       <description><c>["foo","bar"]</c> or <c>[1,2]</c> ...</description>
    ///     </item>
    ///     <item>
    ///       <term>Dictionary</term>
    ///       <description><c>{foo:"bar",b:10}</c></description>
    ///     </item>
    ///     <item>
    ///       <term>Object</term>
    ///       <description><c>{foo:"bar",b:10}</c></description>
    ///     </item>
    ///   </list>
    /// </summary>
    /// <param name="value"></param>
    /// <returns>The formatted query param.</returns>
    /// <exception cref="InvalidDataException">Invalid Object Type in Param List</exception>
    protected internal virtual string FormatQueryParam(object? value)
    {
        switch (value)
        {
            case null:
                return "null";

            case string strValue:
                string encoded = strValue.Replace("\"", "\\\"");
                return $"\"{encoded}\"";

            case char charValue:
                return $"\"{charValue}\"";

            case byte byteValue:
                return byteValue.ToString();

            case sbyte sbyteValue:
                return sbyteValue.ToString();

            case short shortValue:
                return shortValue.ToString();

            case ushort ushortValue:
                return ushortValue.ToString();

            case int intValue:
                return intValue.ToString();

            case uint uintValue:
                return uintValue.ToString();

            case long longValue:
                return longValue.ToString();

            case ulong ulongValue:
                return ulongValue.ToString();

            case float floatValue:
                return floatValue.ToString(CultureInfo.CreateSpecificCulture("en-us"));

            case double doubleValue:
                return doubleValue.ToString(CultureInfo.CreateSpecificCulture("en-us"));

            case decimal decimalValue:
                return decimalValue.ToString(CultureInfo.CreateSpecificCulture("en-us"));

            case bool booleanValue:
                return booleanValue ? "true" : "false";
            
            case RawString rawStringValue:
                return rawStringValue.Value;

            case Enum enumValue:
                return enumValue.ToString();

            case DateTime dateTimeValue:
                return this.FormatQueryParam(dateTimeValue.ToString("o"));

            case KeyValuePair<string, object> kvValue:
                return $"{kvValue.Key}:{this.FormatQueryParam(kvValue.Value)}";

            case IDictionary<string, object> dictValue:
                return $"{{{string.Join(",", dictValue.Select(e => this.FormatQueryParam(e)))}}}";

            case IEnumerable enumerableValue:
                List<string> items = [];
                foreach (object item in enumerableValue)
                {
                    items.Add(this.FormatQueryParam(item));
                }
                return $"[{string.Join(",", items)}]";

            case { } objectValue:
                Dictionary<string, object> dictionay = this.ObjectToDictionary(objectValue);
                return this.FormatQueryParam(dictionay);

            default:
                throw new InvalidDataException($"Invalid Object Type in Param List: {value.GetType()}");
        }
    }

    /// <summary>Adds query params to the query string.</summary>
    /// <param name="query">The query.</param>
    protected internal void AddParams<TSource>(IQuery<TSource> query)
    {
        RequiredArgument.NotNull(query, nameof(query));

        foreach (KeyValuePair<string, object?> param in query.Arguments)
        {
            this.QueryString.Append($"{param.Key}:{this.FormatQueryParam(param.Value)},");
        }

        if (query.Arguments.Count > 0)
        {
            this.QueryString.Length--;
        }
    }

    /// <summary>Adds fields to the query sting.</summary>
    /// <param name="query">The query.</param>
    /// <exception cref="ArgumentException">Invalid Object in Field List</exception>
    protected internal void AddFields<TSource>(IQuery<TSource> query)
    {
        foreach (object? item in query.SelectList)
        {
            switch (item)
            {
                case string field:
                    this.QueryString.Append($"{field} ");
                    break;

                case IQuery subQuery:
                    this.QueryString.Append($"{subQuery.Build()} ");
                    break;

                default:
                    throw new ArgumentException("Invalid Field Type Specified, must be `string` or `Query`");
            }
        }

        if (query.SelectList.Count > 0)
        {
            this.QueryString.Length--;
        }
    }

    /// <summary>Convert object into dictionary.</summary>
    /// <param name="object">The object.</param>
    /// <returns>The object as dictionary.</returns>
    private Dictionary<string, object> ObjectToDictionary(object @object) =>
        @object
            .GetType()
            .GetProperties()
            .Where(property => property.GetValue(@object) != null)
            .Select(property =>
                new KeyValuePair<string, object>(
                    this.formatter is not null ? this.formatter.Invoke(property) : property.Name,
                    property.GetValue(@object)))
            .OrderBy(property => property.Key)
            .ToDictionary(property => property.Key, property => property.Value);
}
