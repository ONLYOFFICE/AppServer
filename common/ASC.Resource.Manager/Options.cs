﻿using CommandLine;

namespace ASC.Resource.Manager
{
    public class Options
    {
        [Option('p', "project", Required = false, HelpText = "Project")]
        public string Project { get; set; }

        [Option('m', "module", Required = false, HelpText = "Module")]
        public string Module { get; set; }

        [Option('e', "exportpath", Required = false, HelpText = "Export Path", Default = "..\\..\\..\\..\\ASC.Common\\")]
        public string ExportPath { get; set; }

        [Option('c', "culture", Required = false, HelpText = "Culture")]
        public string Culture { get; set; }

        [Option('f', "format", Required = false, HelpText = "Format", Default = "xml")]
        public string Format { get; set; }

        public void Deconstruct(out string project, out string module, out string exportPath, out string culture, out string format) 
            => (project, module, exportPath, culture, format) = (Project, Module, ExportPath, Culture, Format);
    }
}
