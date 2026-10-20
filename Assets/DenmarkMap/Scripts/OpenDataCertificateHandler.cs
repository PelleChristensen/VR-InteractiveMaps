using UnityEngine.Networking;

namespace DenmarkMap
{
    /// <summary>
    /// Accepts the server certificate for the public, read-only open-data feeds (Energi Data Service, Open-Meteo).
    /// School and company networks often inspect HTTPS with their own certificate, which Android apps reject by
    /// default, so the map would show no data there. Only use this for public data that is never sent anywhere.
    /// </summary>
    public class OpenDataCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;

        /// <summary>Configures a request for an open-data feed.</summary>
        public static UnityWebRequest Get(string url)
        {
            var r = UnityWebRequest.Get(url);
            r.certificateHandler = new OpenDataCertificateHandler();
            r.disposeCertificateHandlerOnDispose = true;
            return r;
        }
    }
}
