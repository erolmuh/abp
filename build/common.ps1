$full = $args[0]

# COMMON PATHS 

$rootFolder = (Get-Item -Path "./" -Verbose).FullName

# List of solutions used only in development mode
$solutionPaths = @(
		"../framework",
		"../modules/basic-theme",
		"../modules/users",
		"../modules/permission-management",
		"../modules/setting-management",
		"../modules/feature-management",
		"../modules/identity",
		"../modules/identityserver",
		"../modules/openiddict",
		"../modules/tenant-management",
		"../modules/audit-logging",
		"../modules/background-jobs",
		"../modules/account",
		"../modules/cms-kit",
		"../modules/blob-storing-database"
	)

# Remove MAUI related projects if not on Windows
if ($env:OS -ne "Windows_NT") {
	dotnet sln ../framework/Volo.Abp.sln remove ../framework/src/Volo.Abp.AspNetCore.Components.MauiBlazor.Bundling/Volo.Abp.AspNetCore.Components.MauiBlazor.Bundling.csproj
}

if ($full -eq "-f")
{
	# List of additional solutions required for full build
	$solutionPaths += (
		"../modules/client-simulation",
		"../modules/virtual-file-explorer",
		"../modules/docs",
		"../modules/blogging",
		"../templates/module/aspnet-core",
		"../templates/app/aspnet-core",
		"../templates/console",
		"../templates/app-nolayers/aspnet-core",
		"../abp_io/AbpIoLocalization",
		"../source-code"
	)
	if ($env:OS -eq "Windows_NT") {
		$solutionPaths += "../templates/wpf"
	}
}else{ 
	Write-host ""
	Write-host ":::::::::::::: !!! You are in development mode !!! ::::::::::::::" -ForegroundColor red -BackgroundColor  yellow
	Write-host "" 
} 
