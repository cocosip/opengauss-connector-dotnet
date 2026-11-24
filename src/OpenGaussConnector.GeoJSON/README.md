# OpenGaussConnector.GeoJSON

GeoJSON plugin for the OpenGaussConnector ADO.NET provider. It maps PostGIS `geometry` and `geography`
values to the types of [GeoJSON.NET](https://github.com/GeoJSON-Net/GeoJSON.Net).

To use the plugin, add a dependency on this package and configure the type mapper at startup:

```csharp
OpenGaussConnection.GlobalTypeMapper.UseGeoJson();
```

After that, GeoJSON geometry types can be read and written like any other value:

```csharp
var point = new Point(new Position(8.5, 47.3));

using (var cmd = new OpenGaussCommand("INSERT INTO cities (location) VALUES (@p)", conn))
{
    cmd.Parameters.Add(new OpenGaussParameter("p", point));
    cmd.ExecuteNonQuery();
}
```

`UseGeoJson` accepts the following options:

| Option | Meaning |
| --- | --- |
| `GeoJSONOptions.None` | Default: no bounding box, no CRS (RFC 7946 output) |
| `GeoJSONOptions.BoundingBox` | Include the bounding box of each geometry |
| `GeoJSONOptions.ShortCRS` / `LongCRS` | Include the legacy `crs` member, in short or full URN form |

The `geographyAsDefault` argument decides which PostGIS type the plugin registers for by default.

The plugin keeps the coordinates it reads; it does not reproject them. Values labelled RFC 7946 are
expected to use WGS 84 longitude/latitude. Reading or writing these types requires the PostGIS
extension and a matching `geometry`/`geography` type in the database.
