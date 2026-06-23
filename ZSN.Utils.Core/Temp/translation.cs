using System;
using System.Collections;

namespace Goheer
{
	namespace EXIF
	{
		/// <summary>
		/// Summary description for translation.
		/// </summary>
		public class translation : Hashtable
		{
			/// <summary>
			/// 
			/// </summary>
			public translation()
			{
                this.Add(0x8769, "Exif IFD");
                this.Add(0x8825, "Gps IFD");
                this.Add(0xFE, "New Subfile Type");
                this.Add(0xFF, "Subfile Type");
                this.Add(0x100, "Image Width"); 
                this.Add(0x101, "Image Height"); 
                this.Add(0x102, "Bits Per Sample");
                this.Add(0x103, "Compression");
                this.Add(0x106, "Photometric Interp");
                this.Add(0x107, "Thresh Holding");
                this.Add(0x108, "Cell Width");
                this.Add(0x109, "Cell Height");
                this.Add(0x10A, "Fill Order");
                this.Add(0x10D, "Document Name");
                this.Add(0x10E, "Image Description");
                this.Add(0x10F, "Equip Make"); 
                this.Add(0x110, "Equip Model");
                this.Add(0x111, "Strip Offsets");
                this.Add(0x112, "Orientation");
                this.Add(0x115, "Samples PerPixel");
                this.Add(0x116, "Rows Per Strip");
                this.Add(0x117, "Strip Bytes Count");
                this.Add(0x118, "Min Sample Value");
                this.Add(0x119, "Max Sample Value");
                this.Add(0x11A, "X Resolution");
                this.Add(0x11B, "Y Resolution");
                this.Add(0x11C, "Planar Config");
                this.Add(0x11D, "Page Name");
                this.Add(0x11E, "X Position");
                this.Add(0x11F, "Y Position");
                this.Add(0x120, "Free Offset");
                this.Add(0x121, "Free Byte Counts");
                this.Add(0x122, "Gray Response Unit");
                this.Add(0x123, "Gray Response Curve");
                this.Add(0x124, "T4 Option");
                this.Add(0x125, "T6 Option");
                this.Add(0x128, "Resolution Unit");
                this.Add(0x129, "Page Number");
                this.Add(0x12D, "Transfer Funcition");
                this.Add(0x131, "Software Used");
                this.Add(0x132, "Date Time");
                this.Add(0x13B, "Artist");
                this.Add(0x13C, "Host Computer");
                this.Add(0x13D, "Predictor");
                this.Add(0x13E, "White Point");
                this.Add(0x13F, "Primary Chromaticities");
                this.Add(0x140, "ColorMap");
                this.Add(0x141, "Halftone Hints");
                this.Add(0x142, "Tile Width");
                this.Add(0x143, "Tile Length");
                this.Add(0x144, "Tile Offset");
                this.Add(0x145, "Tile ByteCounts");
                this.Add(0x14C, "InkSet");
                this.Add(0x14D, "Ink Names");
                this.Add(0x14E, "Number Of Inks");
                this.Add(0x150, "Dot Range");
                this.Add(0x151, "Target Printer");
                this.Add(0x152, "Extra Samples");
                this.Add(0x153, "Sample Format");
                this.Add(0x154, "S Min Sample Value");
                this.Add(0x155, "S Max Sample Value");
                this.Add(0x156, "Transfer Range");
                this.Add(0x200, "JPEG Proc");
                this.Add(0x201, "JPEG InterFormat");
                this.Add(0x202, "JPEG InterLength");
                this.Add(0x203, "JPEG RestartInterval");
                this.Add(0x205, "JPEG LosslessPredictors");
                this.Add(0x206, "JPEG PointTransforms");
                this.Add(0x207, "JPEG QTables");
                this.Add(0x208, "JPEG DCTables");
                this.Add(0x209, "JPEG ACTables");
                this.Add(0x211, "YCbCr Coefficients");
                this.Add(0x212, "YCbCr Subsampling");
                this.Add(0x213, "YCbCr Positioning");
                this.Add(0x214, "REF Black White");
                this.Add(0x8773, "ICC Profile");
                this.Add(0x301, "Gamma");
                this.Add(0x302, "ICC Profile Descriptor");
                this.Add(0x303, "SRGB RenderingIntent");
                this.Add(0x320, "Image Title");
                this.Add(0x8298, "Copyright");
                this.Add(0x5001, "Resolution X Unit");
                this.Add(0x5002, "Resolution Y Unit");
                this.Add(0x5003, "Resolution X LengthUnit");
                this.Add(0x5004, "Resolution Y LengthUnit");
                this.Add(0x5005, "Print Flags");
                this.Add(0x5006, "Print Flags Version");
                this.Add(0x5007, "Print Flags Crop");
                this.Add(0x5008, "Print Flags Bleed Width");
                this.Add(0x5009, "Print Flags Bleed Width Scale");
                this.Add(0x500A, "Halftone LPI");
                this.Add(0x500B, "Halftone LPIUnit");
                this.Add(0x500C, "Halftone Degree");
                this.Add(0x500D, "Halftone Shape");
                this.Add(0x500E, "Halftone Misc");
                this.Add(0x500F, "Halftone Screen");
                this.Add(0x5010, "JPEG Quality");
                this.Add(0x5011, "Grid Size");
                this.Add(0x5012, "Thumbnail Format");
                this.Add(0x5013, "Thumbnail Width");
                this.Add(0x5014, "Thumbnail Height");
                this.Add(0x5015, "Thumbnail ColorDepth");
                this.Add(0x5016, "Thumbnail Planes");
                this.Add(0x5017, "Thumbnail RawBytes");
                this.Add(0x5018, "Thumbnail Size");
                this.Add(0x5019, "Thumbnail CompressedSize");
                this.Add(0x501A, "Color Transfer Function");
                this.Add(0x501B, "Thumbnail Data");
                this.Add(0x5020, "Thumbnail ImageWidth");
                this.Add(0x502, "Thumbnail ImageHeight");
                this.Add(0x5022, "Thumbnail BitsPerSample");
                this.Add(0x5023, "Thumbnail Compression");
                this.Add(0x5024, "Thumbnail PhotometricInterp");
                this.Add(0x5025, "Thumbnail ImageDescription");
                this.Add(0x5026, "Thumbnail EquipMake");
                this.Add(0x5027, "Thumbnail EquipModel");
                this.Add(0x5028, "Thumbnail StripOffsets");
                this.Add(0x5029, "Thumbnail Orientation");
                this.Add(0x502A, "Thumbnail SamplesPerPixel");
                this.Add(0x502B, "Thumbnail RowsPerStrip");
                this.Add(0x502C, "Thumbnail StripBytesCount");
                this.Add(0x502D, "Thumbnail ResolutionX");
                this.Add(0x502E, "Thumbnail ResolutionY");
                this.Add(0x502F, "Thumbnail PlanarConfig");
                this.Add(0x5030, "Thumbnail ResolutionUnit");
                this.Add(0x5031, "Thumbnail TransferFunction");
                this.Add(0x5032, "Thumbnail SoftwareUsed");
                this.Add(0x5033, "Thumbnail DateTime");
                this.Add(0x5034, "Thumbnail Artist");
                this.Add(0x5035, "Thumbnail WhitePoint");
                this.Add(0x5036, "Thumbnail PrimaryChromaticities");
                this.Add(0x5037, "Thumbnail YCbCrCoefficients");
                this.Add(0x5038, "Thumbnail YCbCrSubsampling");
                this.Add(0x5039, "Thumbnail YCbCrPositioning");
                this.Add(0x503A, "Thumbnail RefBlackWhite");
                this.Add(0x503B, "Thumbnail CopyRight");
                this.Add(0x5090, "Luminance Table");
                this.Add(0x5091, "Chrominance Table");
                this.Add(0x5100, "Frame Delay");
                this.Add(0x5101, "Loop Count");
                this.Add(0x5110, "Pixel Unit");
                this.Add(0x5111, "Pixel PerUnit X");
                this.Add(0x5112, "Pixel PerUnit Y");
                this.Add(0x5113, "Palette Histogram");
                this.Add(0x829A, "Exposure Time");
                this.Add(0x829D, "F-Number"); 
                this.Add(0x8822, "Exposure Prog");
                this.Add(0x8824, "Spectral Sense");
                this.Add(0x8827, "ISO Speed"); 
                this.Add(0x8828, "OECF");
                this.Add(0x9000, "Exif Ver");
                this.Add(0x9003, "DTOrig");
                this.Add(0x9004, "DTDigitized");
                this.Add(0x9101, "CompConfig");
                this.Add(0x9102, "CompBPP");
                this.Add(0x9201, "Shutter Speed");
                this.Add(0x9202, "Aperture");
                this.Add(0x9203, "Brightness");
                this.Add(0x9204, "Exposure Bias");
                this.Add(0x9205, "MaxAperture");
                this.Add(0x9206, "SubjectDist");
                this.Add(0x9207, "Metering Mode");
                this.Add(0x9208, "LightSource");
                this.Add(0x9209, "Flash");
                this.Add(0x920A, "FocalLength");
                this.Add(0x927C, "Maker Note");
                this.Add(0x9286, "User Comment");
                this.Add(0x9290, "DTSubsec");
                this.Add(0x9291, "DTOrigSS");
                this.Add(0x9292, "DTDigSS");
                this.Add(0xA000, "FPXVer");
                this.Add(0xA001, "ColorSpace");
                this.Add(0xA002, "PixXDim");
                this.Add(0xA003, "PixYDim");
                this.Add(0xA004, "RelatedWav");
                this.Add(0xA005, "Interop");
                this.Add(0xA20B, "FlashEnergy");
                this.Add(0xA20C, "SpatialFR");
                this.Add(0xA20E, "FocalXRes");
                this.Add(0xA20F, "FocalYRes");
                this.Add(0xA210, "FocalResUnit");
                this.Add(0xA214, "Subject Loc");
                this.Add(0xA215, "Exposure Index");
                this.Add(0xA217, "Sensing Method");
                this.Add(0xA300, "FileSource");
                this.Add(0xA301, "SceneType");
                this.Add(0xA302, "CfaPattern");
                this.Add(0x0, "Gps Ver");
                this.Add(0x1, "Gps LatitudeRef");
                this.Add(0x2, "Gps Latitude");
                this.Add(0x3, "Gps LongitudeRef");
                this.Add(0x4, "Gps Longitude");
                this.Add(0x5, "Gps AltitudeRef");
                this.Add(0x6, "Gps Altitude");
                this.Add(0x7, "Gps GpsTime");
                this.Add(0x8, "Gps GpsSatellites");
                this.Add(0x9, "Gps GpsStatus");
                this.Add(0xA, "Gps GpsMeasureMode");
                this.Add(0xB, "Gps GpsDop");
                this.Add(0xC, "Gps SpeedRef");
                this.Add(0xD, "Gps Speed");
                this.Add(0xE, "Gps TrackRef");
                this.Add(0xF, "Gps Track");
                this.Add(0x10, "Gps ImgDirRef");
                this.Add(0x11, "Gps ImgDir");
                this.Add(0x12, "Gps MapDatum");
                this.Add(0x13, "Gps DestLatRef");
                this.Add(0x14, "Gps DestLat");
                this.Add(0x15, "Gps DestLongRef");
                this.Add(0x16, "Gps DestLong");
                this.Add(0x17, "Gps DestBearRef");
                this.Add(0x18, "Gps DestBear");
                this.Add(0x19, "Gps DestDistRef");
                this.Add(0x1A, "Gps DestDist");
			}
		}
		/// <summary>
		/// private class
		/// </summary>
		internal class Rational
		{
			private int n;
			private int d;
			public Rational(int n, int d)
			{
				this.n = n;
				this.d = d;
				simplify(ref this.n, ref this.d);
			}
			public Rational(uint n, uint d)
			{
				this.n = Convert.ToInt32(n);
				this.d = Convert.ToInt32(d);

				simplify(ref this.n, ref this.d);
			}
			public Rational()
			{
				this.n= this.d=0;
			}
			public string ToString(string sp)
			{
				if( sp == null ) sp = "/";
				return n.ToString() + sp + d.ToString();
			}
			public double ToDouble()
			{
				if( d == 0 )
					return 0.0;

				return Math.Round(Convert.ToDouble(n)/Convert.ToDouble(d),2);
			}
			private void simplify( ref int a, ref int b )
			{
				if( a== 0 || b == 0 )
					return;

				int	gcd = euclid(a,b);
				a /= gcd;
				b /= gcd;
			}
			private int euclid(int a, int b)
			{
				if(b==0)	
					return a;
				else		
					return euclid(b,a%b);
			}
		}
	}
}