using System.Buffers.Binary;
using System.IO;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

if (args.Length != 2)
{
    Console.Error.WriteLine("Uso: IconGenerator <svg> <ico>");
    return 2;
}

var svg = XDocument.Load(args[0]);
var pathData = svg.Descendants().FirstOrDefault(element => element.Name.LocalName == "path")?.Attribute("d")?.Value;
if (string.IsNullOrWhiteSpace(pathData))
{
    Console.Error.WriteLine("O SVG não contém um caminho Material Symbols válido.");
    return 1;
}

var geometry = Geometry.Parse(pathData);
var pngs = new[] { 16, 32, 48, 256 }.Select(size => RenderPng(geometry, size)).ToArray();
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
using var output = File.Create(args[1]);
WriteUInt16(output, 0);
WriteUInt16(output, 1);
WriteUInt16(output, (ushort)pngs.Length);

var offset = 6 + pngs.Length * 16;
for (var index = 0; index < pngs.Length; index++)
{
    var size = new[] { 16, 32, 48, 256 }[index];
    var png = pngs[index];
    output.WriteByte(size == 256 ? (byte)0 : (byte)size);
    output.WriteByte(size == 256 ? (byte)0 : (byte)size);
    output.WriteByte(0);
    output.WriteByte(0);
    WriteUInt16(output, 1);
    WriteUInt16(output, 32);
    WriteUInt32(output, (uint)png.Length);
    WriteUInt32(output, (uint)offset);
    offset += png.Length;
}

foreach (var png in pngs)
{
    output.Write(png);
}

return 0;

static byte[] RenderPng(Geometry geometry, int size)
{
    var visual = new DrawingVisual();
    using (var drawing = visual.RenderOpen())
    {
        drawing.PushTransform(new MatrixTransform(new Matrix(size / 960d, 0, 0, size / 960d, 0, size)));
        drawing.DrawGeometry(new SolidColorBrush(Color.FromRgb(0, 103, 192)), null, geometry);
    }

    var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return stream.ToArray();
}

static void WriteUInt16(Stream stream, ushort value)
{
    Span<byte> bytes = stackalloc byte[2];
    BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
    stream.Write(bytes);
}

static void WriteUInt32(Stream stream, uint value)
{
    Span<byte> bytes = stackalloc byte[4];
    BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
    stream.Write(bytes);
}
