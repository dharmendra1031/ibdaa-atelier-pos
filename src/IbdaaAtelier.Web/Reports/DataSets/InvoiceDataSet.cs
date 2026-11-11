using System.Data;
using System.Xml;

namespace IbdaaAtelier.Web.Reports.DataSets;

public sealed class InvoiceDataSet : DataSet
{
    public InvoiceDataSet(string schemaPath)
    {
        using var reader = XmlReader.Create(schemaPath);
        ReadXmlSchema(reader);
    }

    public DataTable Header => Tables["InvoiceHeader"]!;
    public DataTable Lines => Tables["InvoiceLines"]!;
    public DataTable Payments => Tables["InvoicePayments"]!;
}
