using System.Xml.Linq;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 SOAP API'ye credential-aware XML çağrıları yapan client.
/// Her istekte MarketPlace tablosundan (Id=2) appKey/appSecret çeker
/// ve SOAP envelope içindeki auth bloğu olarak ekler.
/// </summary>
public interface IN11SoapClient
{
    /// <summary>
    /// Belirtilen WSDL endpoint'ine SOAP request gönderir.
    /// </summary>
    /// <param name="wsdlPath">Servis yolu, örneğin "CategoryService"</param>
    /// <param name="soapAction">SOAP action ismi (N11 için boş string gönderilebilir)</param>
    /// <param name="bodyContent">SOAP Body içindeki XML elementi (auth otomatik eklenir)</param>
    /// <returns>Response SOAP body'sinin içeriğini XElement olarak döner</returns>
    Task<XElement> SendAsync(string wsdlPath, string soapAction, XElement bodyContent);
}
