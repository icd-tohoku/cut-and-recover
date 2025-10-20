using System.Xml;
using System.Xml.Serialization;
namespace VeryAnimation.grendgine_collada
{
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Grendgine_Collada_Joints
    {

        [XmlElement(ElementName = "input")]
        public Grendgine_Collada_Input_Unshared[] Input;

        [XmlElement(ElementName = "extra")]
        public Grendgine_Collada_Extra[] Extra;
    }
}

//check done