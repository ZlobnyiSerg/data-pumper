using DataPumper.Core;
using System;
using System.Globalization;
using System.Linq;

namespace DataPumper.PostgreSql;

internal class CleanupTableRequestAdapter
{
    private readonly CleanupTableRequest _request;

    public CleanupTableRequestAdapter(CleanupTableRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        DataSource = new DataSourceAdapter(request.DataSource);
    }

    public DataSourceAdapter DataSource { get; }

    public string ActualityFieldName => _request.ActualityFieldName?.Enquote();

    public DateTime? LastLoadDate => _request.LastLoadDate;

    public DateTime CurrentPropertyDate => _request.CurrentPropertyDate;

    public string HistoricColumnsFrom => _request.HistoricColumnsFrom?.Enquote();

    public string HistoricColumnsTo => _request.HistoricColumnsTo?.Enquote();

    public string FilterPredicate
    {
        get
        {
            if (_request.Filter == null) return null;
            return string.Join(" AND ",
                _request.Filter.Select(f => InClause(f.FieldName, f.Values))
                    .Where(x => x != null));
        }
    }

    public string TenantFilter => InClause(_request.TenantField, _request.TenantCodes);

    public string DeleteProtectionDateFilter
    {
        get
        {
            if (_request.DeleteProtectionDate == null) return null;

            var dateString = _request.DeleteProtectionDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return $"{ActualityFieldName} >= cast('{dateString}' as timestamp)";
        }
    }

    private static string InClause(string fieldName, string[] values)
    {
        if (string.IsNullOrEmpty(fieldName) || values?.Any() != true)
            return null;

        return $"{fieldName.Enquote()} IN ({string.Join(", ", values.Select(v => v.Enquote('\'')))})";
    }
}