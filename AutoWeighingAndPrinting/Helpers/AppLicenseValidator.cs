using System;

namespace AutoWeighingAndPrinting.Helpers
{

    public class LicenseDetail
    {
        public DateTime CreationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysLeft { get; set; }
        public bool IsLicenseValid { get; set; }
    }

    public enum LicenseStatus
    {
        NewUser = 1,
        Expired,
        ChangeLicense,
        NotValid
    }
    public class AppLicenseValidator
    {

        public LicenseDetail GetAppLicenseDetails(string licenseKey)
        {
            try
            {
                var licenseDetail = new LicenseDetail();

                if (string.IsNullOrEmpty(licenseKey))
                    return null;


                SKGL.Validate validate = new SKGL.Validate();
                validate.secretPhase = Environment.MachineName.ToLowerInvariant();
                validate.Key = licenseKey;

                licenseDetail.IsLicenseValid = validate.IsValid;

                if (!validate.IsValid)
                {
                    return licenseDetail;
                }


                licenseDetail.CreationDate = validate.CreationDate;
                licenseDetail.ExpiryDate = validate.ExpireDate;
                licenseDetail.DaysLeft = validate.DaysLeft;


                return licenseDetail;

            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
