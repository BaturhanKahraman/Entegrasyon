using System.Globalization;
using System.Xml.Linq;
using Entegrasyon.Entity.Invoicing;
using EInvoiceEntity = Entegrasyon.Entity.Invoicing.EInvoice;
using EInvoiceLineEntity = Entegrasyon.Entity.Invoicing.EInvoiceLine;

namespace Entegrasyon.Business.Utility.EInvoice;

/// <summary>
/// GIB UBL-TR 1.2 formatinda fatura XML'i olusturur.
/// Dogrulama yapmaz — dogrulama business katmaninda FluentValidation ile yapilir.
/// </summary>
public static class UblTrXmlBuilder
{
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Inv = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    public static string Build(EInvoiceEntity invoice)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            BuildInvoiceElement(invoice));

        return doc.Declaration + Environment.NewLine + doc.Root;
    }

    private static XElement BuildInvoiceElement(EInvoiceEntity invoice)
    {
        var root = new XElement(Inv + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", Cac.NamespaceName),
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "TR1.2"),
            new XElement(Cbc + "ProfileID", invoice.InvoiceType == EInvoiceType.EFatura ? "TICARIFATURA" : "EARSIVFATURA"),
            new XElement(Cbc + "ID", invoice.InvoiceNumber),
            new XElement(Cbc + "CopyIndicator", "false"),
            new XElement(Cbc + "UUID", invoice.GibUuid ?? Guid.NewGuid().ToString()),
            new XElement(Cbc + "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(Cbc + "InvoiceTypeCode", invoice.InvoiceType == EInvoiceType.EFatura ? "SATIS" : "SATIS"),
            new XElement(Cbc + "DocumentCurrencyCode", "TRY"),
            BuildAccountingCustomerParty(invoice),
            BuildTaxTotal(invoice),
            BuildLegalMonetaryTotal(invoice));

        foreach (var line in invoice.Lines)
        {
            root.Add(BuildInvoiceLine(line));
        }

        return root;
    }

    private static XElement BuildAccountingCustomerParty(EInvoiceEntity invoice)
    {
        return new XElement(Cac + "AccountingCustomerParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", invoice.CustomerTaxId.Length == 11 ? "TCKN" : "VKN"),
                        invoice.CustomerTaxId)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", invoice.CustomerTitle))));
    }

    private static XElement BuildTaxTotal(EInvoiceEntity invoice)
    {
        return new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", "TRY"),
                invoice.TaxAmount.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private static XElement BuildLegalMonetaryTotal(EInvoiceEntity invoice)
    {
        return new XElement(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", "TRY"),
                invoice.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(Cbc + "TaxExclusiveAmount",
                new XAttribute("currencyID", "TRY"),
                invoice.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(Cbc + "TaxInclusiveAmount",
                new XAttribute("currencyID", "TRY"),
                invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(Cbc + "PayableAmount",
                new XAttribute("currencyID", "TRY"),
                invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private static XElement BuildInvoiceLine(EInvoiceLineEntity line)
    {
        return new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", line.Id.ToString()),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", "C62"),
                line.Quantity.ToString(CultureInfo.InvariantCulture)),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", "TRY"),
                (line.UnitPrice * line.Quantity).ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(Cac + "TaxTotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", "TRY"),
                    line.TaxAmount.ToString("F2", CultureInfo.InvariantCulture)),
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount",
                        new XAttribute("currencyID", "TRY"),
                        (line.UnitPrice * line.Quantity).ToString("F2", CultureInfo.InvariantCulture)),
                    new XElement(Cbc + "TaxAmount",
                        new XAttribute("currencyID", "TRY"),
                        line.TaxAmount.ToString("F2", CultureInfo.InvariantCulture)),
                    new XElement(Cbc + "Percent", line.TaxRate.ToString(CultureInfo.InvariantCulture)),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cac + "TaxScheme",
                            new XElement(Cbc + "Name", "KDV"),
                            new XElement(Cbc + "TaxTypeCode", "0015"))))),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Name", line.ProductName)),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", "TRY"),
                    line.UnitPrice.ToString("F2", CultureInfo.InvariantCulture))));
    }
}
