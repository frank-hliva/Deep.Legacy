module Deep.Text

open System.IO

let extensions = Set [".js";".json";".css";".html";".htm";".xml";".txt";".csv";".config";".md"]

let isTextFile (fileName : string) =
    extensions.Contains(Path.GetExtension(fileName).ToLower())