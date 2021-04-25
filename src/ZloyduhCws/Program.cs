using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZloyduhCws
{
    public class Program
    {
        private const string RunGcodeFileName = "run.gcode";
        private const string PreviewFileName = "preview.png";
        private const string PreviewCroppingFileName = "preview_cropping.png";

        private const string SlicingFileName = "default.slicing";
        private const string GcodeFileName = "1.gcode";
        private const string ManifestFileName = "manifest.xml";

        //arg0 - Tmp ZIP folder - C:/Users/<user_name>/AppData/Local/ChiTuBox/zipTmpDir/
        //arg1 - Final output folder
        public static void Main(string[] args)
        {
            //args = new string[] { "a1b2c3_0.stl" };

            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: too few args - crapping the fuck out!");
                Environment.Exit(0);
            }

            var tmpDirPath = args[0];

            var valueMap = ReadRunGcode($"{tmpDirPath}/{RunGcodeFileName}");

            File.Delete($"{tmpDirPath}/{RunGcodeFileName}");
            File.Delete($"{tmpDirPath}/{PreviewFileName}");
            File.Delete($"{tmpDirPath}/{PreviewCroppingFileName}");

            WriteSlicingFile($"{tmpDirPath}/{SlicingFileName}", valueMap);
            WriteGcodeFile($"{tmpDirPath}/{GcodeFileName}", valueMap);
            WriteManifestXmlFile(tmpDirPath, $"{tmpDirPath}/{ManifestFileName}", valueMap);
        }

        private static Dictionary<string, string> ReadRunGcode(string filePath)
        {
            var fileLines = File.ReadAllLines(filePath);

            var result = new Dictionary<string, string>();
            foreach (var fileLine in fileLines)
            {
                if (fileLine.StartsWith(";START_GCODE_BEGIN"))
                    break;

                var split = fileLine.Split(':');
                var key = split[0].Replace(";", "");
                result.Add(key, split[1]);
            }

            return result;
        }

        private static void WriteSlicingFile(string filePath, Dictionary<string, string> valueMap)
        {
            var resolutionX = Convert.ToInt32(valueMap["resolutionX"]);
            var resolutionY = Convert.ToInt32(valueMap["resolutionY"]);
            var widthX = Convert.ToDouble(valueMap["machineX"]);
            var widthY = Convert.ToDouble(valueMap["machineY"]);

            var fileLines = new List<string>
            {
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                "<SliceBuildConfig FileVersion=\"2\">",
                $"<DotsPermmX>{resolutionX / widthX}</DotsPermmX>",
                $"<DotsPermmY>{resolutionY / widthY}</DotsPermmY>",
                $"<XResolution>{valueMap["resolutionX"]}</XResolution>",
                $"<YResolution>{valueMap["resolutionY"]}</YResolution>",
                $"<BlankTime>{Convert.ToInt32(Convert.ToDouble(valueMap["normalExposureTime"]) * 1000)}</BlankTime>",
                "<PlatformTemp>75</PlatformTemp>",
                "<ExportSVG>0</ExportSVG>",
                "<Export>True</Export>",
                "<ExportPNG>False</ExportPNG>",
                "<XOffset>0</XOffset>",
                "<YOffset>0</YOffset>",
                "<Direction>Bottom_Up</Direction>",
                $"<LiftDistance>{valueMap["normalLayerLiftHeight"]}</LiftDistance>",
                "<SlideTiltValue>0</SlideTiltValue>",
                "<AntiAliasing>False</AntiAliasing>",
                "<UseMainLiftGCode>False</UseMainLiftGCode>",
                "<AntiAliasingValue>0</AntiAliasingValue>",
                $"<LiftFeedRate>{Convert.ToInt32(valueMap["normalLayerLiftSpeed"])}</LiftFeedRate>",
                $"<BottomLiftFeedRate>{Convert.ToInt32(valueMap["bottomLayerLiftSpeed"])}</BottomLiftFeedRate>",
                $"<LiftRetractRate>{Convert.ToInt32(valueMap["normalDropSpeed"])}</LiftRetractRate>",
                "<ExportOption>ZIP</ExportOption>",
                "<RenderOutlines>False</RenderOutlines>",
                "<OutlineWidth_Inset>0</OutlineWidth_Inset>",
                "<OutlineWidth_Outset>0</OutlineWidth_Outset>",
                $"<FlipX>{valueMap["mirror"] == "1"}</FlipX>",
                "<FlipY>False</FlipY>",
                "<Notes>I'm Pickle Rick mother fuckers!!!</Notes>",
                // ----- GCodeHeader ----- //
                "<GCodeHeader>",
                "; ********** Header Start ********",
                "; Here you can set any G or M - Code which should be executed BEFORE the build process",
                "G21 ;Set units to be mm",
                "G91 ;Relative positioning",
                "M17 ;Enable motors",
                "M106 S255",
                "; ********** Header End **********",
                "</GCodeHeader>",
                // ----- GCodeFooter ----- //
                "<GCodeFooter>",
                "; ********** Footer Start ********",
                "; Here you can set any G or M - Code which should be executed after the last Layer is Printed",
                "M106 S0",
                "G1 Z100.0 F150.0",
                "G04 P30000",
                "M18 ;Disable Motors",
                "; ********** Footer End ********",
                "</GCodeFooter>",
                // ----- GCodePreslice ----- //
                "<GCodePreslice>",
                "; ********** Pre - Slice Start ********",
                "; Set up any GCode here to be executed before a lift",
                "; ********** Pre - Slice End **********",
                "</GCodePreslice>",
                // ----- GCodeLift ----- //
                "<GCodeLift>",
                "; ********** Lift Sequence Start ********",
                "M106 S0",
                "G1{$SlideTiltVal != 0? X$SlideTiltVal:} Z($ZLiftDist * $ZDir) F{$CURSLICE &lt; $NumFirstLayers?$ZBottomLiftRate:$ZLiftRate}",
                "G1{$SlideTiltVal != 0? X($SlideTiltVal * -1):} Z(($LayerThickness-$ZLiftDist) * $ZDir) F$ZRetractRate",
                ";&lt;Delay&gt; %d$BlankTime",
                "M106 S255",
                "; ********** Lift Sequence End **********",
                "</GCodeLift>",
                // ----- GCodeLayer ----- //
                "<GCodeLayer>",
                ";********** Layer Start ********",
                ";Here you can set any G or M-Code which should be executed per-layer during the build process",
                "&lt;slice&gt; $CURSLICE",
                "G91 ;Relative Positioning",
                "M17 ;Enable motors",
                ";********** Layer End **********",
                "</GCodeLayer>",
                //------------------------//
                "<SelectedInk>Default</SelectedInk>",
                "<InkConfig>",
                "<Name>Default</Name>",
                $"<SliceHeight>{valueMap["layerHeight"]}</SliceHeight>",
                $"<LayerTime>{Convert.ToInt32(Convert.ToDouble(valueMap["normalExposureTime"]) * 1000)}</LayerTime>",
                $"<FirstLayerTime>{Convert.ToInt32(Convert.ToDouble(valueMap["bottomLayExposureTime"]) * 1000)}</FirstLayerTime>",
                $"<NumberofBottomLayers>{valueMap["bottomLayCount"]}</NumberofBottomLayers>",
                "<ResinPriceL>0</ResinPriceL>",
                "</InkConfig>",
                "<MinTestExposure>500</MinTestExposure>",
                "<TestExposureStep>200</TestExposureStep>",
                "<ExportPreview>None</ExportPreview>",
                "<UserParameters/>",
                "</SliceBuildConfig>"
            };

            File.WriteAllLines(filePath, fileLines);
        }

        private static void WriteGcodeFile(string filePath, Dictionary<string, string> valueMap)
        {
            int layerNum = Convert.ToInt32(valueMap["totalLayer"]);
            int bottomLayerNum = Convert.ToInt32(valueMap["bottomLayCount"]);
            int normalExposureTime = Convert.ToInt32(Convert.ToDouble(valueMap["normalExposureTime"]) * 1000);
            int bottomLayExposureTime = Convert.ToInt32(Convert.ToDouble(valueMap["bottomLayExposureTime"]) * 1000);
            int bottomLightOffTime = Convert.ToInt32(Convert.ToDouble(valueMap["bottomLightOffTime"]) * 1000);
            int lightOffTime = Convert.ToInt32(Convert.ToDouble(valueMap["lightOffTime"]) * 1000);
            int resolutionX = Convert.ToInt32(valueMap["resolutionX"]);
            int resolutionY = Convert.ToInt32(valueMap["resolutionY"]);
            double widthX = Convert.ToDouble(valueMap["machineX"]);
            double widthY = Convert.ToDouble(valueMap["machineY"]);
            double layerHeight = Convert.ToDouble(valueMap["layerHeight"]);
            double normalLayerLiftHeight = Convert.ToDouble(valueMap["normalLayerLiftHeight"]);
            double dropHeight = normalLayerLiftHeight - layerHeight;

            var fileLines = new List<string>
            {
                ";(****Build and Slicing Parameters****)",
                ";(Pix per mm X = " + resolutionX / widthX + " px/mm)",
                ";(Pix per mm Y = " + resolutionY / widthY + " px/mm)",
                ";(X Resolution = " + resolutionX + ")",
                ";(Y Resolution = " + resolutionY + ")",
                ";(Layer Thickness = " + layerHeight + " mm)",
                $";(Layer Time = {normalExposureTime} ms)",
                ";(Render Outlines = False",
                ";(Outline Width Inset = 2",
                ";(Outline Width Outset = 0",
                ";(Bottom Layers Time = " + bottomLayExposureTime + " ms)",
                ";(Number of Bottom Layers = " + valueMap["bottomLayCount"] + ")",
                ";(Blanking Layer Time = " + Convert.ToInt32(Convert.ToDouble(valueMap["normalExposureTime"]) * 1000) +
                " ms)",
                ";(Build Direction = Bottom_Up)",
                ";(Lift Distance = " + valueMap["normalLayerLiftHeight"] + " mm)",
                ";(Slide/Tilt Value = 0)",
                ";(Anti Aliasing = False)",
                ";(Use Mainlift GCode Tab = False)",
                ";(Anti Aliasing Value = 0)",
                $";(Z Lift Feed Rate = {Convert.ToInt32(valueMap["normalLayerLiftSpeed"])} mm/s)",
                $";(Z Bottom Lift Feed Rate = {Convert.ToInt32(valueMap["bottomLayerLiftSpeed"])} mm/s )",
                $";(Z Lift Retract Rate = {Convert.ToInt32(valueMap["normalDropSpeed"])} mm/s)",
                ";(Flip X = " + (valueMap["mirror"] == "1" ? "True" : "False") + ")",
                ";(Flip Y = False)",
                ";Number of Slices = " + valueMap["totalLayer"],
                ";(****Machine Configuration ******)",
                ";(Platform X Size = " + valueMap["machineX"] + "mm)",
                ";(Platform Y Size = " + valueMap["machineY"] + "mm)",
                ";(Platform Z Size = " + valueMap["machineZ"] + "mm)",
                ";(Max X Feedrate = 100mm/s)",
                ";(Max Y Feedrate = 100mm/s)",
                ";(Max Z Feedrate = 100mm/s)",
                ";(Machine Type = UV_DLP)",
                "; ********** Header Start ********",
                "; Here you can set any G or M - Code which should be executed BEFORE the build process",
                "G21; Set units to be mm",
                "G91; Relative Positioning",
                "M17; Enable motors",
                "M106 S255",
                "; ********** Header End **********"
            };

            for (int c = 0; c < layerNum; c++)
            {
                fileLines.Add(";********** Pre-Slice Start ********");
                fileLines.Add(";Set up any GCode here to be executed before a lift");

                fileLines.Add(";********** Pre-Slice End **********");
                fileLines.Add(";<Slice> " + c);
                fileLines.Add(";<Delay> " + (c < bottomLayerNum ? bottomLayExposureTime : normalExposureTime));
                fileLines.Add(";<Slice> Blank");

                fileLines.Add("; ********** Lift Sequence ********");
                fileLines.Add("M106 S0");
                fileLines.Add("G1 Z" + valueMap["normalLayerLiftHeight"] + " F" + (c < bottomLayerNum ? valueMap["bottomLayerLiftSpeed"] : valueMap["normalLayerLiftSpeed"]));
                fileLines.Add("G1 Z-" + dropHeight + " F" + valueMap["normalDropSpeed"]);
                fileLines.Add(";<Delay> " + (c < bottomLayerNum ? bottomLightOffTime : lightOffTime));
                fileLines.Add("M106 S255");
            }

            fileLines.Add("; ********** Footer Start ********");
            fileLines.Add("; Here you can set any G or M - Code which should be executed after the last Layer is Printed");
            fileLines.Add("M106 S0");
            fileLines.Add("G1 Z100.0 F150.0");
            fileLines.Add("G04 P30000");
            fileLines.Add("M18 ;Disable Motors");
            fileLines.Add("; ********** Footer End ********");

            File.WriteAllLines(filePath, fileLines);
        }

        private static void WriteManifestXmlFile(string dirPath, string filePath, Dictionary<string, string> valueMap)
        {
            var fileLines = new List<string>
            {
                "<?xml version = \"1.0\" encoding = \"utf-8\"?>",
                "<manifest FileVersion = \"1\">"
            };

            var stlFileName = valueMap["fileName"].Split('.')[0];
            stlFileName = $"z{string.Concat(stlFileName.Where(char.IsLetter))}";

            fileLines.Add("<Models>");
            fileLines.Add("  <model>");
            fileLines.Add($"    <name>{stlFileName}_0001</name>");
            fileLines.Add("    <tag>0</tag>");
            fileLines.Add("  </model>");
            fileLines.Add("  <model>");
            fileLines.Add($"    <name>{stlFileName}_Base_0002</name>");
            fileLines.Add("    <tag>3</tag>");
            fileLines.Add("  </model>");
            fileLines.Add("</Models>");

            fileLines.Add("<Slices>");

            var pngFileNames = Directory.GetFiles(dirPath, "*.png").ToList().Select(f => f.Substring(f.LastIndexOf('/') + 1)).ToList();
            var longestFileName = pngFileNames.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);

            var pngFiles = new SortedList<int, string>();
            foreach (var pngFileName in pngFileNames)
            {
                var newFileName = pngFileName;
                for (int c = pngFileName.Length; c <= longestFileName.Length; ++c)
                    newFileName = $"0{newFileName}";
                newFileName = $"{stlFileName}{newFileName}";

                File.Move($"{dirPath}/{pngFileName}", $"{dirPath}/{newFileName}");

                var fileNum = Convert.ToInt32(pngFileName.Replace(".png", ""));
                pngFiles.Add(fileNum, newFileName);
            }

            foreach (var fileName in pngFiles)
            {
                fileLines.Add("  <Slice>");
                fileLines.Add($"   <name>{fileName.Value}</name>");
                fileLines.Add("  </Slice>");
            }

            fileLines.Add("</Slices>");

            fileLines.Add("<VectorSlices/>");

            fileLines.Add("<SliceProfile>");
            fileLines.Add($"  <name>{SlicingFileName}</name>");
            fileLines.Add("</SliceProfile>");

            fileLines.Add("<GCode>");
            fileLines.Add($"  <name>{GcodeFileName}</name>");
            fileLines.Add("</GCode>");

            fileLines.Add("</manifest>");

            File.WriteAllLines(filePath, fileLines);
        }
    }
}