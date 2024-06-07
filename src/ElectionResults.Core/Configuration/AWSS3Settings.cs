namespace ElectionResults.Core.Configuration
{
    public class AWSS3Settings
    {
        public const string SectionKey = "S3Bucket";
        public string AccessKeyId { get; set; }
        public string AccessKeySecret { get; set; }
        public string BucketName { get; set; }
    }
}