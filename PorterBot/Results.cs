using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace PorterBot
{
    [Serializable]
    class IdentificationResults
    {
        public string Name { get; set; }
        public double? Age { get; set; }//? означает, что переменная может принимать значение "null"
        public Blur Blur { get; set; }
        public Emotion Emotion { get; set; }
        public Exposure Exposure { get; set; }
        public FacialHair FacialHair { get; set; }
        public Gender? Gender { get; set; }
        public GlassesType? Glasses { get; set; }
        public Hair Hair { get; set; }
        public HeadPose HeadPose { get; set; }
        public Makeup Makeup { get; set; }
        public Noise Noise { get; set; }
        public Occlusion Occlusion { get; set; }
        public double? Smile { get; set; }

    }
}
