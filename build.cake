//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.5.0";
var displayVersion = "3.5.0";

//////////////////////////////////////////////////////////////////////
// NUGET PACKAGES
//////////////////////////////////////////////////////////////////////

var CONSOLE_PACKAGES = new []
{
  "NUnit.ConsoleRunner"
};

var EXTENSION_PACKAGES = new []
{
  "NUnit.Extension.VSProjectLoader",
  "NUnit.Extension.NUnitProjectLoader",
  "NUnit.Extension.NUnitV2Driver",
  "NUnit.Extension.NUnitV2ResultWriter",
  "NUnit.Extension.TeamCityEventListener"
};

//////////////////////////////////////////////////////////////////////
// FILE PATHS
//////////////////////////////////////////////////////////////////////

var ROOT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var WIX_PROJ = ROOT_DIR + "nunit/nunit.wixproj";
var RESOURCES_DIR = ROOT_DIR + "resources/";
var RUNNER_PACKAGES_DIR = ROOT_DIR + "runner-packages/";
var EXTENSION_PACKAGES_DIR = ROOT_DIR + "extension-packages/";
var DISTRIBUTION_DIR = ROOT_DIR + "distribution/";
var IMAGE_DIR = ROOT_DIR + "image/";
var IMAGE_ADDINS_DIR = IMAGE_DIR + "addins/";
var ZIP_FILE = string.Format("{0}NUnit.{1}.zip", DISTRIBUTION_DIR, version);

//////////////////////////////////////////////////////////////////////
// TASK
//////////////////////////////////////////////////////////////////////

Task("Clean")
.Does(() =>
{
    CleanDirectory(RUNNER_PACKAGES_DIR);
    CleanDirectory(EXTENSION_PACKAGES_DIR);
    CleanDirectory(IMAGE_DIR);
    CleanDirectory(DISTRIBUTION_DIR);
});

Task("FetchPackages")
.IsDependentOn("Clean")
.Does(() =>
{
    foreach(var package in CONSOLE_PACKAGES)
    {
        NuGetInstall(package, new NuGetInstallSettings { 
						OutputDirectory = RUNNER_PACKAGES_DIR
					});
    }

    foreach(var package in EXTENSION_PACKAGES)
    {
        NuGetInstall(package, new NuGetInstallSettings { 
						OutputDirectory = EXTENSION_PACKAGES_DIR
					});
    }
});

Task("CreateImage")
.IsDependentOn("Clean")
.IsDependentOn("FetchPackages")
.Does(() =>
{
    CopyDirectory(RESOURCES_DIR, IMAGE_DIR);

    foreach(var packageDir in GetAllDirectories(RUNNER_PACKAGES_DIR))
		CopyPackageContents(packageDir, IMAGE_DIR);

    foreach(var packageDir in GetAllDirectories(EXTENSION_PACKAGES_DIR))
		CopyPackageContents(packageDir, IMAGE_ADDINS_DIR);
});

Task("PackageMsi")
.IsDependentOn("CreateImage")
.Does(() =>
{
    MSBuild(WIX_PROJ, new MSBuildSettings()
        .WithTarget("Rebuild")
        .SetConfiguration(configuration)
        .WithProperty("Version", version)
        .WithProperty("DisplayVersion", displayVersion)
        .WithProperty("OutDir", DISTRIBUTION_DIR)
        .WithProperty("Image", IMAGE_DIR)
        .SetMSBuildPlatform(MSBuildPlatform.x86)
        .SetNodeReuse(false)
        );
});

Task("PackageZip")
.IsDependentOn("CreateImage")
.Does(() =>
{
    Zip(IMAGE_DIR, ZIP_FILE);
});

Task("PackageAll")
.IsDependentOn("PackageMsi")
.IsDependentOn("PackageZip");

Task("Appveyor")
.IsDependentOn("PackageAll");

Task("Default")
.IsDependentOn("PackageAll");

//////////////////////////////////////////////////////////////////////
// HELPER METHODS
//////////////////////////////////////////////////////////////////////

public string[] GetAllDirectories(string dirPath)
{
    return System.IO.Directory.GetDirectories(dirPath);
}

public void CopyPackageContents(DirectoryPath packageDir, DirectoryPath outDir)
{
    var tools = packageDir + "/tools";
	CopyDirectory(tools, outDir);
}

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
