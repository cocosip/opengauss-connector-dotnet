# OpenGaussConnector.Json.NET

Json.NET plugin for the OpenGaussConnector ADO.NET provider. It lets CLR types be stored in and read
from the `json` and `jsonb` types through [Newtonsoft.Json](https://www.newtonsoft.com/json).

To use the plugin, add a dependency on this package and configure the type mapper at startup:

```csharp
OpenGaussConnection.GlobalTypeMapper.UseJsonNet();
```

After that, JSON values can be read and written like any other value:

```csharp
using (var cmd = new OpenGaussCommand("INSERT INTO documents (data) VALUES (@p)", conn))
{
    cmd.Parameters.Add(new OpenGaussParameter("p", myPoco));
    cmd.ExecuteNonQuery();
}
```

`UseJsonNet` accepts:

| Argument | Meaning |
| --- | --- |
| `jsonbClrTypes` | CLR types to map to `jsonb` without declaring `OpenGaussDbType.Jsonb` |
| `jsonClrTypes` | CLR types to map to `json` without declaring `OpenGaussDbType.Json` |
| `settings` | `JsonSerializerSettings` used for both serialization and deserialization |

Without an explicit list, values of the CLR types the built-in JSON handler already understands (`string`,
`char[]`, `char`, `ArraySegment<char>` and `byte[]`) keep going through that handler; every other CLR type is
serialized with Json.NET.

The serializer settings used here are Json.NET's defaults unless you supply your own, and they do not write
`$type`. A payload read from the database is therefore deserialized into the type the column is mapped to, and
cannot name an arbitrary type to instantiate. If a caller needs polymorphic payloads, pass settings that enable
`TypeNameHandling` and follow the usual Json.NET guidance about which types the serializer may create.
