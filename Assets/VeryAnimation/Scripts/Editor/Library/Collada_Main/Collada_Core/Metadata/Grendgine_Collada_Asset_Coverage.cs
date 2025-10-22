using System.Xml;
using System.Xml.Serialization;
#pragma warning disable 0169
namespace VeryAnimation.grendgine_collada
{
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Grendgine_Collada_Asset_Coverage
    {
        [XmlElement(ElementName = "geographic_location")]
#pragma warning disable IDE0044, IDE0051
        Grendgine_Collada_Geographic_Location Geographic_Location;
#pragma warning restore IDE0051, IDE0044 
    }
}

//check done