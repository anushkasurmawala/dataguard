namespace DataGuard.Api.Models;
public class Dataset
{
    public Guid Id
    {
        get;
        set;
    }
 = Guid.NewGuid();
    public string FileName
    {
        get;
        set;
    }
 = "";
    public string DatasetType
    {
        get;
        set;
    }
 = "";
    public DateTime UploadedAt
    {
        get;
        set;
    }
 = DateTime.UtcNow;
    public string FilePath
    {
        get;
        set;
    }
 = "";
    public string ProfileJson
    {
        get;
        set;
    }
 = "";
}
public class Comparison
{
    public Guid Id
    {
        get;
        set;
    }
 = Guid.NewGuid();
    public Guid BaselineDatasetId
    {
        get;
        set;
    }
    public Guid CurrentDatasetId
    {
        get;
        set;
    }
    public Dataset BaselineDataset
    {
        get;
        set;
    }
 = null!;
    public Dataset CurrentDataset
    {
        get;
        set;
    }
 = null!;
    public DateTime CreatedAt
    {
        get;
        set;
    }
 = DateTime.UtcNow;
    public string ResultJson
    {
        get;
        set;
    }
 = "";
}
