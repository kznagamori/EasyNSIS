using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace EasyNSIS.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        // Enable window dragging
        MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

        LoadAssemblyInfo();
        LoadLicenses();
    }

    private void LoadAssemblyInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Get version
        var version = assembly.GetName().Version;
        VersionText.Text = $"Version {version}";
        FileVersionText.Text = version?.ToString() ?? "1.0.0.0";

        // Get company
        var companyAttr = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
        CompanyText.Text = companyAttr?.Company ?? "Unknown";

        // Get copyright
        var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
        CopyrightText.Text = copyrightAttr?.Copyright ?? "Unknown";
    }

    private void LoadLicenses()
    {
        var licenses = new List<LicenseInfo>();
        var assembly = Assembly.GetExecutingAssembly();

        // Define library info with their embedded resource names
        var libraryInfo = new Dictionary<string, (string DisplayName, string LicenseType)>
        {
            { "License_CommunityToolkit.Mvvm.md", ("CommunityToolkit.Mvvm", "MIT License") },
            { "LICENSE_Microsoft.Extensions.DependencyInjection.TXT", ("Microsoft.Extensions.DependencyInjection", "MIT License") },
            { "LICENSE_Microsoft.Xaml.Behaviors.Wpf.txt", ("Microsoft.Xaml.Behaviors.Wpf", "MIT License") },
            { "LICENSE_UTF.Unknown.txt", ("UTF.Unknown", "MIT License / MPL 1.1 / LGPL") }
        };

        // Get all embedded resource names
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            // Check if this is a license file
            foreach (var (fileName, (displayName, licenseType)) in libraryInfo)
            {
                if (resourceName.EndsWith(fileName))
                {
                    var licenseText = ReadEmbeddedResource(assembly, resourceName);
                    if (!string.IsNullOrEmpty(licenseText))
                    {
                        licenses.Add(new LicenseInfo
                        {
                            Name = displayName,
                            LicenseType = licenseType,
                            LicenseText = licenseText
                        });
                    }
                    break;
                }
            }
        }

        // Add PresentationFramework.Fluent (built-in, MIT License)
        licenses.Insert(0, new LicenseInfo
        {
            Name = "PresentationFramework.Fluent",
            LicenseType = "MIT License",
            LicenseText = "Windows Presentation Foundation Fluent theme.\n" +
                         "Part of the .NET Framework, licensed under the MIT License.\n\n" +
                         "Copyright (c) .NET Foundation and Contributors\n\n" +
                         "Permission is hereby granted, free of charge, to any person obtaining a copy\n" +
                         "of this software and associated documentation files (the \"Software\"), to deal\n" +
                         "in the Software without restriction, including without limitation the rights\n" +
                         "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell\n" +
                         "copies of the Software, and to permit persons to whom the Software is\n" +
                         "furnished to do so, subject to the following conditions:\n\n" +
                         "The above copyright notice and this permission notice shall be included in all\n" +
                         "copies or substantial portions of the Software.\n\n" +
                         "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\n" +
                         "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\n" +
                         "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT."
        });

        LicenseList.ItemsSource = licenses;
    }

    private static string ReadEmbeddedResource(Assembly assembly, string resourceName)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return string.Empty;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class LicenseInfo
{
    public string Name { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string LicenseText { get; set; } = string.Empty;
}
