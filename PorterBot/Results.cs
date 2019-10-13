using System;
using System.Runtime.Serialization;

namespace PorterBot
{
    [DataContract]
    public class IdentificationResults
    {
        [DataMember]
        public string Age { get; set; }//? означает, что переменная может принимать значение "null"
        [DataMember]
        public string Blur { get; set; }
        [DataMember]
        public PorterEmotion PorterEmotion { get; set; }
        [DataMember]
        public string Exposure { get; set; }
        [DataMember]
        public FacialHair FacialHair { get; set; }
        [DataMember]
        public string Gender { get; set; }
        [DataMember]
        public string Glasses { get; set; }
        [DataMember]
        public Hair Hair { get; set; }
        [DataMember]
        public string HeadPose { get; set; }
        [DataMember]
        public string Makeup { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Noise { get; set; }
        [DataMember]
        public string Occlusion { get; set; }
        [DataMember]
        public string Smile { get; set; }
    }

    public class PorterEmotion
    {
        public string Anger { get; set; }
        public string Contempt { get; set; }
        public string Disgust { get; set; }
        public string Fear { get; set; }
        public string Happiness { get; set; }
        public string Neutral { get; set; }
        public string Sadness { get; set; }
        public string Surprise { get; set; }
    }

    public class FacialHair
    {
        public string Beard { get; set; }
        public string Moustache { get; set; }
        public string Sideburns { get; set; }
    }

    public class Hair
    {
        public string Bald { get; set; }
        public string Invisible { get; set; }
        public PorterHairColor HairColor { get; set; }
    }

    public class PorterHairColor
    {
        public string[] ColorConfidence = new string[6];
    }

}
