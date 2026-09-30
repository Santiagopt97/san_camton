using System.Text;
using ClientesApi.Data;
using ClientesApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClientesApi.Controllers;

[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin,recepcion")]
[Route("api/reportes/clientes")]
public class ReportesController(ClientesDbContext db) : ControllerBase
{
    private async Task<List<Cliente>> Datos(string? q, bool soloActivos)
    {
        var query = db.Clientes.AsNoTracking().AsQueryable();
        if (soloActivos) query = query.Where(c => c.Activo);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var p = $"%{q.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.Nombres, p) || EF.Functions.ILike(c.Apellidos, p)
                || EF.Functions.ILike(c.NumeroDocumento, p));
        }
        return await query.OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres).ToListAsync();
    }

    [HttpGet("csv")]
    public async Task<IActionResult> Csv([FromQuery] string? q, [FromQuery] bool soloActivos = false)
    {
        var rows = await Datos(q, soloActivos);
        static string E(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        var sb = new StringBuilder();
        sb.AppendLine("Documento,Nombres,Apellidos,Email,Telefono,Nacionalidad,Estado");
        foreach (var c in rows)
            sb.AppendLine(string.Join(",", E($"{c.TipoDocumento} {c.NumeroDocumento}"), E(c.Nombres), E(c.Apellidos),
                E(c.Email), E(c.Telefono), E(c.Nacionalidad), E(c.Activo ? "Activo" : "Inactivo")));
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"clientes_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf([FromQuery] string? q, [FromQuery] bool soloActivos = false)
    {
        var rows = await Datos(q, soloActivos);
        static IContainer Cell(IContainer c) => c.BorderBottom(1).BorderColor("#E8DFD0").Padding(5);
        static IContainer Head(IContainer c) => c.Background("#0F2D3D").Padding(5);

        var pdf = Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4.Landscape());
            p.Margin(30);
            p.DefaultTextStyle(t => t.FontSize(10).FontColor("#1F2933"));
            p.Header().Column(col =>
            {
                col.Item().Text("Reporte de clientes").FontSize(20).Bold().FontColor("#0F2D3D");
                col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm} · Total: {rows.Count}")
                    .FontColor("#6B7280");
                col.Item().PaddingTop(4).LineHorizontal(2).LineColor("#C49A5A");
            });
            p.Content().PaddingTop(12).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(3);
                    c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1.2f);
                });
                t.Header(h =>
                {
                    foreach (var name in new[] { "Documento", "Nombre", "Email", "Teléfono", "Nacionalidad", "Estado" })
                        h.Cell().Element(Head).Text(name).Bold().FontColor("#FFFFFF");
                });
                foreach (var c in rows)
                {
                    t.Cell().Element(Cell).Text($"{c.TipoDocumento} {c.NumeroDocumento}");
                    t.Cell().Element(Cell).Text($"{c.Nombres} {c.Apellidos}");
                    t.Cell().Element(Cell).Text(c.Email ?? "—");
                    t.Cell().Element(Cell).Text(c.Telefono ?? "—");
                    t.Cell().Element(Cell).Text(c.Nacionalidad ?? "—");
                    t.Cell().Element(Cell).Text(c.Activo ? "Activo" : "Inactivo")
                        .FontColor(c.Activo ? "#4F7A5A" : "#B85450");
                }
            });
            p.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ").FontColor("#6B7280"); x.CurrentPageNumber();
                x.Span(" / ").FontColor("#6B7280"); x.TotalPages();
            });
        })).GeneratePdf();

        return File(pdf, "application/pdf", $"clientes_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
