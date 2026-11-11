// Gera o ícone do app a partir de assets/icons/updater.svg:
//   - o .ico multi-resolução (entradas PNG, fundo transparente);
//   - o logo PNG de 128 px usado na interface.
// Uso: dotnet run --project tools/IconGenerator -- assets/icons/updater.svg assets/icons/Atualizador.ico assets/icons/updater-128.png
// Lê somente elementos <path> com d, fill, stroke, stroke-width, stroke-linecap e stroke-linejoin.
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

int[] icoSizes = [16, 20, 24, 32, 40, 48, 64, 256];
const int LogoSize = 128;
// Fração do quadro ocupada pelo desenho. Em 16–24 px a margem é menor para o desenho ficar legível.
const double SmallFill = 0.98;
const double RegularFill = 0.90;

if (args.Length != 3)
{
    Console.Error.WriteLine("Uso: IconGenerator <svg> <ico> <png-128>");
    return 2;
}

var shapes = ReadShapes(XDocument.Load(args[0]));
if (shapes.Count == 0)
{
    Console.Error.WriteLine("O SVG não contém elementos <path> com desenho.");
    return 1;
}

var bounds = Rect.Empty;
foreach (var shape in shapes)
{
    bounds.Union(shape.Geometry.GetRenderBounds(shape.Pen));
}

var pngs = icoSizes.Select(size => RenderPng(shapes, bounds, size, size <= 24 ? SmallFill : RegularFill)).ToArray();
WriteIco(args[1], icoSizes, pngs);
WriteFile(args[2], RenderPng(shapes, bounds, LogoSize, RegularFill));
return 0;

static List<Shape> ReadShapes(XDocument svg)
{
    var shapes = new List<Shape>();
    foreach (var element in svg.Descendants().Where(element => element.Name.LocalName == "path"))
    {
        var data = (string?)element.Attribute("d");
        if (string.IsNullOrWhiteSpace(data))
        {
            continue;
        }

        var fill = ParseBrush((string?)element.Attribute("fill") ?? "#000000");
        var stroke = ParseBrush((string?)element.Attribute("stroke") ?? "none");
        Pen? pen = null;
        if (stroke is not null)
        {
            var width = double.Parse((string?)element.Attribute("stroke-width") ?? "1", CultureInfo.InvariantCulture);
            var cap = ParseCap((string?)element.Attribute("stroke-linecap"));
            pen = new Pen(stroke, width)
            {
                StartLineCap = cap,
                EndLineCap = cap,
                LineJoin = ParseJoin((string?)element.Attribute("stroke-linejoin")),
            };
            pen.Freeze();
        }

        shapes.Add(new Shape(Geometry.Parse(data), fill, pen));
    }

    return shapes;
}

static Brush? ParseBrush(string value)
{
    if (value == "none")
    {
        return null;
    }

    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    brush.Freeze();
    return brush;
}

static PenLineCap ParseCap(string? value) => value switch
{
    "round" => PenLineCap.Round,
    "square" => PenLineCap.Square,
    _ => PenLineCap.Flat,
};

static PenLineJoin ParseJoin(string? value) => value switch
{
    "round" => PenLineJoin.Round,
    "bevel" => PenLineJoin.Bevel,
    _ => PenLineJoin.Miter,
};

// Centraliza o desenho em um quadro quadrado e o escala para ocupar a fração "fill" do lado.
static byte[] RenderPng(IReadOnlyList<Shape> shapes, Rect bounds, int size, double fill)
{
    var scale = size * fill / Math.Max(bounds.Width, bounds.Height);
    var offsetX = size / 2d - (bounds.X + bounds.Width / 2) * scale;
    var offsetY = size / 2d - (bounds.Y + bounds.Height / 2) * scale;

    var visual = new DrawingVisual();
    using (var drawing = visual.RenderOpen())
    {
        drawing.PushTransform(new MatrixTransform(scale, 0, 0, scale, offsetX, offsetY));
        foreach (var shape in shapes)
        {
            drawing.DrawGeometry(shape.Fill, shape.Pen, shape.Geometry);
        }
    }

    var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return stream.ToArray();
}

static void WriteIco(string path, int[] sizes, byte[][] pngs)
{
    using var output = CreateFile(path);
    WriteUInt16(output, 0);
    WriteUInt16(output, 1);
    WriteUInt16(output, (ushort)pngs.Length);

    var offset = 6 + pngs.Length * 16;
    for (var index = 0; index < pngs.Length; index++)
    {
        // No cabeçalho do ICO, 0 significa 256 px.
        var dimension = sizes[index] >= 256 ? (byte)0 : (byte)sizes[index];
        output.WriteByte(dimension);
        output.WriteByte(dimension);
        output.WriteByte(0);
        output.WriteByte(0);
        WriteUInt16(output, 1);
        WriteUInt16(output, 32);
        WriteUInt32(output, (uint)pngs[index].Length);
        WriteUInt32(output, (uint)offset);
        offset += pngs[index].Length;
    }

    foreach (var png in pngs)
    {
        output.Write(png);
    }
}

static void WriteFile(string path, byte[] content)
{
    using var output = CreateFile(path);
    output.Write(content);
}

static FileStream CreateFile(string path)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    return File.Create(path);
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

internal sealed record Shape(Geometry Geometry, Brush? Fill, Pen? Pen);
