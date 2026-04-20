using System;
using System.IO;
using System.Text.RegularExpressions;

var path = "Services/WorkspaceService.cs";
var code = File.ReadAllText(path);

// Find all $"""...""" and replace with 345"""...""" then replace literal curly braces with single ones, and interpolation ones with double ones.
// This is somewhat tricky. Let's just do targeted replacements for the known template strings.

code = code.Replace("mainCs = $\"\"\"", "mainCs = 345\"\"\"");

// We need to double the curly braces ONLY for the interpolated variables in these blocks.
// They are {projectName} and {dirName}
code = code.Replace("{projectName}", "{{projectName}}");
code = code.Replace("{dirName}", "{{dirName}}");

// But wait, the CSPROJ string might also use {projectName}. Let's check it.
// If it uses {projectName}, it was also wrapped in $""". That is fine, it will become 345""" and {{projectName}}.

File.WriteAllText(path, code);
Console.WriteLine("Fixed.");
