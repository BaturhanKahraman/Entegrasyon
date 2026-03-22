using System.Xml.Linq;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 SOAP API'ye credential-aware XML cagrilari yapan client.
/// Her istekte MarketPlace tablosundan (Id=2) appKey/appSecret ceker
/// ve SOAP envelope icindeki auth blogu olarak ekler.
/// </summary>
public interface IN11SoapClient
{
    /// <summary>
    /// Belirtilen WSDL endpoint'ine SOAP request gonderir.
    /// </summary>
    /// <param name="wsdlPath">Servis yolu, ornegin "CategoryService"</param>
    /// <param name="soapAction">SOAP action ismi (N11 icin bos string gonderilebilir)</param>
    /// <param name="bodyContent">SOAP Body icindeki XML elementi (auth otomatik eklenir)</param>
    /// <returns>Response SOAP body'sinin icerigini XElement olarak doner</returns>
    Task<XElement> SendAsync(string wsdlPath, string soapAction, XElement bodyContent);
}
