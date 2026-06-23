
/*
  Goheer.EXIFextractor exif = new Goheer.EXIFextractor(Server.MapPath("/") + "/1.jpg", string.Empty, string.Empty);
            try
            {
                StringBuilder builder = new StringBuilder();
                foreach (System.Web.UI.Pair s in exif)
                {
                    if (s.Second.ToString().Trim() == "-" || s.Second.ToString().Trim() == string.Empty || s.Second.ToString().Trim() == "0")
                        continue;
                    builder.Append("<li>");
                    builder.Append(s.First.ToString().Trim() + " : " + s.Second.ToString().Replace("\0", string.Empty).Trim());
                    builder.Append("</li>");
                }

                builder.Append("<br/><b>"+(string)exif.properties["Gps Latitude"]+"</b>");
                Response.Write(builder.ToString());
            }
            finally
            {
                exif = null;
            }
 */

using System;
using Goheer.EXIF;
using System.Text;
using System.Collections;
using System.Drawing.Imaging;
using System.Reflection;
using System.IO;


namespace Goheer
{
    /// <summary>
    /// EXIFextractor Class
    /// 
    /// </summary>
    public class EXIFextractor : IEnumerable
    {
        /// <summary>
        /// Get the individual property value by supplying property name
        /// These are the valid property names :
        /// 
        /// "Exif IFD"
        /// "Gps IFD"
        /// "New Subfile Type"
        /// "Subfile Type"
        /// "Image Width"
        /// "Image Height"
        /// "Bits Per Sample"
        /// "Compression"
        /// "Photometric Interp"
        /// "Thresh Holding"
        /// "Cell Width"
        /// "Cell Height"
        /// "Fill Order"
        /// "Document Name"
        /// "Image Description"
        /// "Equip Make"
        /// "Equip Model"
        /// "Strip Offsets"
        /// "Orientation"
        /// "Samples PerPixel"
        /// "Rows Per Strip"
        /// "Strip Bytes Count"
        /// "Min Sample Value"
        /// "Max Sample Value"
        /// "X Resolution"
        /// "Y Resolution"
        /// "Planar Config"
        /// "Page Name"
        /// "X Position"
        /// "Y Position"
        /// "Free Offset"
        /// "Free Byte Counts"
        /// "Gray Response Unit"
        /// "Gray Response Curve"
        /// "T4 Option"
        /// "T6 Option"
        /// "Resolution Unit"
        /// "Page Number"
        /// "Transfer Funcition"
        /// "Software Used"
        /// "Date Time"
        /// "Artist"
        /// "Host Computer"
        /// "Predictor"
        /// "White Point"
        /// "Primary Chromaticities"
        /// "ColorMap"
        /// "Halftone Hints"
        /// "Tile Width"
        /// "Tile Length"
        /// "Tile Offset"
        /// "Tile ByteCounts"
        /// "InkSet"
        /// "Ink Names"
        /// "Number Of Inks"
        /// "Dot Range"
        /// "Target Printer"
        /// "Extra Samples"
        /// "Sample Format"
        /// "S Min Sample Value"
        /// "S Max Sample Value"
        /// "Transfer Range"
        /// "JPEG Proc"
        /// "JPEG InterFormat"
        /// "JPEG InterLength"
        /// "JPEG RestartInterval"
        /// "JPEG LosslessPredictors"
        /// "JPEG PointTransforms"
        /// "JPEG QTables"
        /// "JPEG DCTables"
        /// "JPEG ACTables"
        /// "YCbCr Coefficients"
        /// "YCbCr Subsampling"
        /// "YCbCr Positioning"
        /// "REF Black White"
        /// "ICC Profile"
        /// "Gamma"
        /// "ICC Profile Descriptor"
        /// "SRGB RenderingIntent"
        /// "Image Title"
        /// "Copyright"
        /// "Resolution X Unit"
        /// "Resolution Y Unit"
        /// "Resolution X LengthUnit"
        /// "Resolution Y LengthUnit"
        /// "Print Flags"
        /// "Print Flags Version"
        /// "Print Flags Crop"
        /// "Print Flags Bleed Width"
        /// "Print Flags Bleed Width Scale"
        /// "Halftone LPI"
        /// "Halftone LPIUnit"
        /// "Halftone Degree"
        /// "Halftone Shape"
        /// "Halftone Misc"
        /// "Halftone Screen"
        /// "JPEG Quality"
        /// "Grid Size"
        /// "Thumbnail Format"
        /// "Thumbnail Width"
        /// "Thumbnail Height"
        /// "Thumbnail ColorDepth"
        /// "Thumbnail Planes"
        /// "Thumbnail RawBytes"
        /// "Thumbnail Size"
        /// "Thumbnail CompressedSize"
        /// "Color Transfer Function"
        /// "Thumbnail Data"
        /// "Thumbnail ImageWidth"
        /// "Thumbnail ImageHeight"
        /// "Thumbnail BitsPerSample"
        /// "Thumbnail Compression"
        /// "Thumbnail PhotometricInterp"
        /// "Thumbnail ImageDescription"
        /// "Thumbnail EquipMake"
        /// "Thumbnail EquipModel"
        /// "Thumbnail StripOffsets"
        /// "Thumbnail Orientation"
        /// "Thumbnail SamplesPerPixel"
        /// "Thumbnail RowsPerStrip"
        /// "Thumbnail StripBytesCount"
        /// "Thumbnail ResolutionX"
        /// "Thumbnail ResolutionY"
        /// "Thumbnail PlanarConfig"
        /// "Thumbnail ResolutionUnit"
        /// "Thumbnail TransferFunction"
        /// "Thumbnail SoftwareUsed"
        /// "Thumbnail DateTime"
        /// "Thumbnail Artist"
        /// "Thumbnail WhitePoint"
        /// "Thumbnail PrimaryChromaticities"
        /// "Thumbnail YCbCrCoefficients"
        /// "Thumbnail YCbCrSubsampling"
        /// "Thumbnail YCbCrPositioning"
        /// "Thumbnail RefBlackWhite"
        /// "Thumbnail CopyRight"
        /// "Luminance Table"
        /// "Chrominance Table"
        /// "Frame Delay"
        /// "Loop Count"
        /// "Pixel Unit"
        /// "Pixel PerUnit X"
        /// "Pixel PerUnit Y"
        /// "Palette Histogram"
        /// "Exposure Time"
        /// "F-Number"
        /// "Exposure Prog"
        /// "Spectral Sense"
        /// "ISO Speed"
        /// "OECF"
        /// "Ver"
        /// "DTOrig"
        /// "DTDigitized"
        /// "CompConfig"
        /// "CompBPP"
        /// "Shutter Speed"
        /// "Aperture"
        /// "Brightness"
        /// "Exposure Bias"
        /// "MaxAperture"
        /// "SubjectDist"
        /// "Metering Mode"
        /// "LightSource"
        /// "Flash"
        /// "FocalLength"
        /// "Maker Note"
        /// "User Comment"
        /// "DTSubsec"
        /// "DTOrigSS"
        /// "DTDigSS"
        /// "FPXVer"
        /// "ColorSpace"
        /// "PixXDim"
        /// "PixYDim"
        /// "RelatedWav"
        /// "Interop"
        /// "FlashEnergy"
        /// "SpatialFR"
        /// "FocalXRes"
        /// "FocalYRes"
        /// "FocalResUnit"
        /// "Subject Loc"
        /// "Exposure Index"
        /// "Sensing Method"
        /// "FileSource"
        /// "SceneType"
        /// "CfaPattern"
        /// "Gps Ver"
        /// "Gps LatitudeRef"
        /// "Gps Latitude"
        /// "Gps LongitudeRef"
        /// "Gps Longitude"
        /// "Gps AltitudeRef"
        /// "Gps Altitude"
        /// "Gps GpsTime"
        /// "Gps GpsSatellites"
        /// "Gps GpsStatus"
        /// "Gps GpsMeasureMode"
        /// "Gps GpsDop"
        /// "Gps SpeedRef"
        /// "Gps Speed"
        /// "Gps TrackRef"
        /// "Gps Track"
        /// "Gps ImgDirRef"
        /// "Gps ImgDir"
        /// "Gps MapDatum"
        /// "Gps DestLatRef"
        /// "Gps DestLat"
        /// "Gps DestLongRef"
        /// "Gps DestLong"
        /// "Gps DestBearRef"
        /// "Gps DestBear"
        /// "Gps DestDistRef"
        /// "Gps DestDist"
        /// </summary>
        public object this[string index]
        {
            get
            {
                return properties[index];
            }
        }
        //
        private System.Drawing.Bitmap bmp;
        //
        private string data;
        //
        private translation myHash;
        //
        public Hashtable properties;
        //
        internal int Count
        {
            get
            {
                return this.properties.Count;
            }
        }
        //
        string sp;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="len"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void setTag(int id, string data)
        {
            Encoding ascii = Encoding.ASCII;
            this.setTag(id, data.Length, 0x2, ascii.GetBytes(data));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="len"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public void setTag(int id, int len, short type, byte[] data)
        {
            PropertyItem p = CreatePropertyItem(type, id, len, data);
            this.bmp.SetPropertyItem(p);
            buildDB(this.bmp.PropertyItems);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="tag"></param>
        /// <param name="len"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static PropertyItem CreatePropertyItem(short type, int tag, int len, byte[] value)
        {
            PropertyItem item;

            // Loads a PropertyItem from a Jpeg image stored in the assembly as a resource.
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream emptyBitmapStream = assembly.GetManifestResourceStream("EXIFextractor.decoy.jpg");
            System.Drawing.Image empty = System.Drawing.Image.FromStream(emptyBitmapStream);

            item = empty.PropertyItems[0];

            // Copies the data to the property item.
            item.Type = type;
            item.Len = len;
            item.Id = tag;
            item.Value = new byte[value.Length];
            value.CopyTo(item.Value, 0);

            return item;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="sp"></param>
        public EXIFextractor(ref System.Drawing.Bitmap bmp, string sp)
        {
            properties = new Hashtable();
            //
            this.bmp = bmp;
            this.sp = sp;
            //
            myHash = new translation();
            buildDB(this.bmp.PropertyItems);
        }
        string msp = "";
        public EXIFextractor(ref System.Drawing.Bitmap bmp, string sp, string msp)
        {
            properties = new Hashtable();
            this.sp = sp;
            this.msp = msp;
            this.bmp = bmp;
            //				
            myHash = new translation();
            this.buildDB(bmp.PropertyItems);

        }
        public static FileStream GetImgFile(string fileName)
        {
            FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }
        public static PropertyItem[] GetExifProperties(string fileName)
        {
            FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                System.Drawing.Image image = System.Drawing.Image.FromStream(stream,
                    /* useEmbeddedColorManagement = */ true,
                    /* validateImageData = */ false);

                return image.PropertyItems;
        }
        public EXIFextractor(string file, string sp, string msp)
        {
            properties = new Hashtable();
            this.sp = sp;
            this.msp = msp;

            myHash = new translation();
            FileStream stream = GetImgFile(file);
            System.Drawing.Image image = System.Drawing.Image.FromStream(stream,
                /* useEmbeddedColorManagement = */ true,
                /* validateImageData = */ false);
            try
            {

                this.buildDB(image.PropertyItems);
            }
            finally {
                image.Dispose();
                stream.Dispose();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void buildDB(System.Drawing.Imaging.PropertyItem[] parr)
        {
            if (parr == null)
                return;
            try
            {
                properties.Clear();
                //
                data = "";
                //
                Encoding ascii = Encoding.ASCII;
                //
                foreach (System.Drawing.Imaging.PropertyItem p in parr)
                {
                    if (p.Value == null)
                        continue;
                    string v = "";
                    string name = (string)myHash[p.Id];
                    // tag not found. skip it
                    if (name == null) continue;
                    //
                    data += name + ": ";
                    //
                    //1 = BYTE An 8-bit unsigned integer.,
                    if (p.Type == 0x1)
                    {
                        v = p.Value[0].ToString();
                    }
                    //2 = ASCII An 8-bit byte containing one 7-bit ASCII code. The final byte is terminated with NULL.,
                    else if (p.Type == 0x2)
                    {
                        // string					
                        v = ascii.GetString(p.Value);
                    }
                    //3 = SHORT A 16-bit (2 -byte) unsigned integer,
                    else if (p.Type == 0x3)
                    {
                        // orientation // lookup table					
                        switch (p.Id)
                        {
                            case 0x8827: // ISO
                                v = "ISO-" + convertToInt16U(p.Value).ToString();
                                break;
                            case 0xA217: // sensing method
                                {
                                    switch (convertToInt16U(p.Value))
                                    {
                                        case 1: v = "Not defined"; break;
                                        case 2: v = "One-chip color area sensor"; break;
                                        case 3: v = "Two-chip color area sensor"; break;
                                        case 4: v = "Three-chip color area sensor"; break;
                                        case 5: v = "Color sequential area sensor"; break;
                                        case 7: v = "Trilinear sensor"; break;
                                        case 8: v = "Color sequential linear sensor"; break;
                                        default: v = " reserved"; break;
                                    }
                                }
                                break;
                            case 0x8822: // aperture 
                                switch (convertToInt16U(p.Value))
                                {
                                    case 0: v = "Not defined"; break;
                                    case 1: v = "Manual"; break;
                                    case 2: v = "Normal program"; break;
                                    case 3: v = "Aperture priority"; break;
                                    case 4: v = "Shutter priority"; break;
                                    case 5: v = "Creative program (biased toward depth of field)"; break;
                                    case 6: v = "Action program (biased toward fast shutter speed)"; break;
                                    case 7: v = "Portrait mode (for closeup photos with the background out of focus)"; break;
                                    case 8: v = "Landscape mode (for landscape photos with the background in focus)"; break;
                                    default: v = "reserved"; break;
                                }
                                break;
                            case 0x9207: // metering mode
                                switch (convertToInt16U(p.Value))
                                {
                                    case 0: v = "unknown"; break;
                                    case 1: v = "Average"; break;
                                    case 2: v = "CenterWeightedAverage"; break;
                                    case 3: v = "Spot"; break;
                                    case 4: v = "MultiSpot"; break;
                                    case 5: v = "Pattern"; break;
                                    case 6: v = "Partial"; break;
                                    case 255: v = "Other"; break;
                                    default: v = "reserved"; break;
                                }
                                break;
                            case 0x9208: // light source
                                {
                                    switch (convertToInt16U(p.Value))
                                    {
                                        case 0: v = "unknown"; break;
                                        case 1: v = "Daylight"; break;
                                        case 2: v = "Fluorescent"; break;
                                        case 3: v = "Tungsten"; break;
                                        case 17: v = "Standard light A"; break;
                                        case 18: v = "Standard light B"; break;
                                        case 19: v = "Standard light C"; break;
                                        case 20: v = "D55"; break;
                                        case 21: v = "D65"; break;
                                        case 22: v = "D75"; break;
                                        case 255: v = "other"; break;
                                        default: v = "reserved"; break;
                                    }
                                }
                                break;
                            case 0x9209:
                                {
                                    switch (convertToInt16U(p.Value))
                                    {
                                        case 0: v = "Flash did not fire"; break;
                                        case 1: v = "Flash fired"; break;
                                        case 5: v = "Strobe return light not detected"; break;
                                        case 7: v = "Strobe return light detected"; break;
                                        default: v = "reserved"; break;
                                    }
                                }
                                break;
                            default:
                                v = convertToInt16U(p.Value).ToString();
                                break;
                        }
                    }
                    //4 = LONG A 32-bit (4 -byte) unsigned integer,
                    else if (p.Type == 0x4)
                    {
                        // orientation // lookup table					
                        v = convertToInt32U(p.Value).ToString();
                    }
                    //5 = RATIONAL Two LONGs. The first LONG is the numerator and the second LONG expresses the//denominator.,
                    else if (p.Type == 0x5)
                    {
                        // rational
                        byte[] n = new byte[p.Len / 2];
                        byte[] d = new byte[p.Len / 2];
                        Array.Copy(p.Value, 0, n, 0, p.Len / 2);
                        Array.Copy(p.Value, p.Len / 2, d, 0, p.Len / 2);
                        uint a = convertToInt32U(n);
                        uint b = convertToInt32U(d);
                        Rational r = new Rational(a, b);
                        //
                        //convert here
                        //
                        byte[] deg = new byte[4];
                        byte[] degdiv = new byte[4];
                        byte[] min = new byte[4];
                        byte[] mindiv = new byte[4];
                        byte[] sec = new byte[4];
                        byte[] secdiv = new byte[4];

                        double a1 ;
                        double b1 ;
                        double c1 ;
                        double d1 ;
                        double e1 ;
                        double f1 ;
                        double o1 ;

                        switch (p.Id)
                        {
                            case 0x9202: // aperture
                                v = "F/" + Math.Round(Math.Pow(Math.Sqrt(2), r.ToDouble()), 2).ToString();
                                break;
                            case 0x920A:
                                v = r.ToDouble().ToString();
                                break;
                            case 0x829A:
                                v = r.ToDouble().ToString();
                                break;
                            case 0x829D: // F-number
                                v = "F/" + r.ToDouble().ToString();
                                break;
                            case 0x0004:
                                //lat
                                Array.Copy(p.Value, 0, deg, 0, 4);
                                Array.Copy(p.Value, 4, degdiv, 0, 4);
                                Array.Copy(p.Value, 8, min, 0, 4);
                                Array.Copy(p.Value, 12, mindiv, 0, 4);
                                Array.Copy(p.Value, 16, sec, 0, 4);
                                Array.Copy(p.Value, 20, secdiv, 0, 4);
                                a1 = convertToInt32U(deg);
                                b1 = convertToInt32U(degdiv);
                                c1 = convertToInt32U(min);
                                d1 = convertToInt32U(mindiv);
                                e1 = convertToInt32U(sec);
                                f1 = convertToInt32U(secdiv);
                                o1 = ((a1 / b1) + ((c1 / b1) / 60) + ((e1 / f1) / 3600));
                                v = o1.ToString();

                                break;
                            case 0x0002:
                                //long
                                Array.Copy(p.Value, 0, deg, 0, 4);
                                Array.Copy(p.Value, 4, degdiv, 0, 4);
                                Array.Copy(p.Value, 8, min, 0, 4);
                                Array.Copy(p.Value, 12, mindiv, 0, 4);
                                Array.Copy(p.Value, 16, sec, 0, 4);
                                Array.Copy(p.Value, 20, secdiv, 0, 4);
                                a1 = convertToInt32U(deg);
                                b1 = convertToInt32U(degdiv);
                                c1 = convertToInt32U(min);
                                d1 = convertToInt32U(mindiv);
                                e1 = convertToInt32U(sec);
                                f1 = convertToInt32U(secdiv);
                                o1 = ((a1 / b1) + ((c1 / b1) / 60) + ((e1 / f1) / 3600));
                                v = o1.ToString();

                                break;
                            default:
                                v = r.ToString("/");
                                break;
                        }

                    }
                    //7 = UNDEFINED An 8-bit byte that can take any value depending on the field definition,
                    else if (p.Type == 0x7)
                    {
                        switch (p.Id)
                        {
                            case 0xA300:
                                {
                                    if (p.Value[0] == 3)
                                    {
                                        v = "DSC";
                                    }
                                    else
                                    {
                                        v = "reserved";
                                    }
                                    break;
                                }
                            case 0xA301:
                                if (p.Value[0] == 1)
                                    v = "A directly photographed image";
                                else
                                    v = "Not a directly photographed image";
                                break;
                            default:
                                v = "-";
                                break;
                        }
                    }
                    //9 = SLONG A 32-bit (4 -byte) signed integer (2's complement notation),
                    else if (p.Type == 0x9)
                    {
                        v = convertToInt32(p.Value).ToString();
                    }
                    //10 = SRATIONAL Two SLONGs. The first SLONG is the numerator and the second SLONG is the
                    //denominator.
                    else if (p.Type == 0xA)
                    {

                        // rational
                        byte[] n = new byte[p.Len / 2];
                        byte[] d = new byte[p.Len / 2];
                        Array.Copy(p.Value, 0, n, 0, p.Len / 2);
                        Array.Copy(p.Value, p.Len / 2, d, 0, p.Len / 2);
                        int a = convertToInt32(n);
                        int b = convertToInt32(d);
                        Rational r = new Rational(a, b);
                        //
                        // convert here
                        //
                        switch (p.Id)
                        {
                            case 0x9201: // shutter speed
                                v = "1/" + Math.Round(Math.Pow(2, r.ToDouble()), 2).ToString();
                                break;
                            case 0x9203:
                                v = Math.Round(r.ToDouble(), 4).ToString();
                                break;
                            default:
                                v = r.ToString("/");
                                break;
                        }
                    }
                    // add it to the list
                    if (properties[name] == null)
                        properties.Add(name, v);
                    // cat it too
                    data += v;
                    data += this.sp;
                }
            }
            finally
            {
                parr = null;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return data;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        int convertToInt32(byte[] arr)
        {
            if (arr.Length != 4)
                return 0;
            else
                return arr[3] << 24 | arr[2] << 16 | arr[1] << 8 | arr[0];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        int convertToInt16(byte[] arr)
        {
            if (arr.Length != 2)
                return 0;
            else
                return arr[1] << 8 | arr[0];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        uint convertToInt32U(byte[] arr)
        {
            if (arr.Length != 4)
            {
                return 0;
            }
            else
            {
                if ((arr[3] << 24 | arr[2] << 16 | arr[1] << 8 | arr[0]) < 0)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToUInt32(arr[3] << 24 | arr[2] << 16 | arr[1] << 8 | arr[0]);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        uint convertToInt16U(byte[] arr)
        {
            if (arr.Length != 2)
                return 0;
            else
                return Convert.ToUInt16(arr[1] << 8 | arr[0]);
        }
        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            // TODO:  Add EXIFextractor.GetEnumerator implementation
            return (new EXIFextractorEnumerator(this.properties));
        }

        #endregion
    }

    //
    // dont touch this class. its for IEnumerator
    // 
    //
    class EXIFextractorEnumerator : IEnumerator
    {
        Hashtable exifTable;
        IDictionaryEnumerator index;

        internal EXIFextractorEnumerator(Hashtable exif)
        {
            this.exifTable = exif;
            this.Reset();
            index = exif.GetEnumerator();
        }

        #region IEnumerator Members

        public void Reset()
        {
            this.index = null;
        }

        public object Current
        {
            get
            {
                return (new System.Web.UI.Pair(this.index.Key, this.index.Value));
            }
        }

        public bool MoveNext()
        {
            if (index != null && index.MoveNext())
                return true;
            else
                return false;
        }

        #endregion

    }
}