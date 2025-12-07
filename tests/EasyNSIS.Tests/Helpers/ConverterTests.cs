using System.Globalization;
using System.Windows;
using System.Windows.Data;
using EasyNSIS.Helpers;
using EasyNSIS.Models;

namespace EasyNSIS.Tests.Helpers;

public class EnumBoolConverterTests
{
    private readonly EnumBoolConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void UT_CNV_001_Convert_MatchingEnum_ReturnsTrue()
    {
        // Arrange
        var value = InstallDestinationType.AppDataRoaming;
        var parameter = InstallDestinationType.AppDataRoaming;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void UT_CNV_002_Convert_NonMatchingEnum_ReturnsFalse()
    {
        // Arrange
        var value = InstallDestinationType.AppDataRoaming;
        var parameter = InstallDestinationType.ProgramFiles;

        // Act
        var result = _converter.Convert(value, typeof(bool), parameter, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void UT_CNV_003_ConvertBack_True_ReturnsParameter()
    {
        // Arrange
        var value = true;
        var parameter = InstallDestinationType.AppDataRoaming;

        // Act
        var result = _converter.ConvertBack(value, typeof(InstallDestinationType), parameter, _culture);

        // Assert
        Assert.Equal(InstallDestinationType.AppDataRoaming, result);
    }

    [Fact]
    public void UT_CNV_004_ConvertBack_False_ReturnsDoNothing()
    {
        // Arrange
        var value = false;
        var parameter = InstallDestinationType.AppDataRoaming;

        // Act
        var result = _converter.ConvertBack(value, typeof(InstallDestinationType), parameter, _culture);

        // Assert
        Assert.Equal(Binding.DoNothing, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(null, typeof(bool), InstallDestinationType.AppDataRoaming, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_NullParameter_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(InstallDestinationType.AppDataRoaming, typeof(bool), null, _culture);

        // Assert
        Assert.False((bool)result);
    }
}

public class InverseBoolConverterTests
{
    private readonly InverseBoolConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void UT_CNV_010_Convert_True_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(true, typeof(bool), null, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void UT_CNV_011_Convert_False_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(false, typeof(bool), null, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void UT_CNV_012_ConvertBack_True_ReturnsFalse()
    {
        // Act
        var result = _converter.ConvertBack(true, typeof(bool), null, _culture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void UT_CNV_013_ConvertBack_False_ReturnsTrue()
    {
        // Act
        var result = _converter.ConvertBack(false, typeof(bool), null, _culture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_NonBool_ReturnsOriginalValue()
    {
        // Act
        var result = _converter.Convert("not a bool", typeof(bool), null, _culture);

        // Assert
        Assert.Equal("not a bool", result);
    }
}

public class InverseBoolToVisibilityConverterTests
{
    private readonly InverseBoolToVisibilityConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void UT_CNV_020_Convert_True_ReturnsCollapsed()
    {
        // Act
        var result = _converter.Convert(true, typeof(Visibility), null, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void UT_CNV_021_Convert_False_ReturnsVisible()
    {
        // Act
        var result = _converter.Convert(false, typeof(Visibility), null, _culture);

        // Assert
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_NonBool_ReturnsCollapsed()
    {
        // Act
        var result = _converter.Convert("not a bool", typeof(Visibility), null, _culture);

        // Assert
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(Visibility.Visible, typeof(bool), null, _culture));
    }
}
