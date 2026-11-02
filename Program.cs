using Stereograms;

var patternFile = args[0];
var templateFile = args[1];
var outputFile = args[2];

var width = StereogramGenerator.Generate(patternFile, templateFile, outputFile);
StereogramReader.Parse(outputFile, width, Path.GetFileNameWithoutExtension(outputFile) + "_dec" + Path.GetExtension(outputFile));
StereogramReader.Parse("animal.jpg", 303, "animal_dec.jpg");