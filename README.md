# Project: Enum Control for Unity

This Unity project is designed to handle enum values in a type-safe manner using the `EnumControl` class. It allows you to manage enum selections efficiently, improving code clarity and performance. The project also focuses on reducing unnecessary allocations, such as Dropdown.OptionData creation, for improved performance in Unity-based UI applications.

## Features

- **Enum-based Control**: The `EnumControl` class provides a UI interface for selecting enum values.
- **Type Safety**: The system ensures type safety for enum handling.
- **Performance Optimization**: Caches enum values and their string names to reduce allocations during runtime.

## Installation

1. Download or clone the repository.
2. Import the necessary scripts into your Unity project.
3. Add the `EnumControl` component to your UI and configure it to work with your enum.

## EnumControl Class

The `EnumControl` class allows you to bind a UI dropdown to an enum type. Here's an example of how to use it:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class ExampleEnumControl : MonoBehaviour
{
    public EnumControl enumControl;

    public enum MyEnum
    {
        Option1,
        Option2,
        Option3
    }

    void Start()
    {
        // Bind the enum to the EnumControl dropdown
        enumControl.SetupEnum<MyEnum>();
    }
}
```

In this example, `EnumControl` will dynamically generate a dropdown based on the `MyEnum` values. The enum values are type-safe and their string representations are cached for better performance.

## Performance Considerations

### Caching Enum Values

The `EnumControl` class caches the enum names and values for efficient access, reducing the overhead of constantly calling `Enum.GetValues()` or `Enum.GetName()` at runtime. This is particularly useful for frequently updated UIs, such as those used in gameplay menus or settings screens.

### Dropdown.OptionData Optimization

To minimize memory allocations, we avoid creating new `Dropdown.OptionData` objects unless necessary. Instead, we utilize a pre-allocated list of `OptionData` that can be reused during UI updates.

### Example: Performance-Optimized Dropdown

```csharp
public class EnumDropdown : MonoBehaviour
{
    public Dropdown dropdown;

    private void Start()
    {
        // Setup enum dropdown without unnecessary allocations
        var enumOptions = Enum.GetValues(typeof(MyEnum))
            .Cast<MyEnum>()
            .Select(e => new Dropdown.OptionData(e.ToString()))
            .ToList();

        dropdown.AddOptions(enumOptions);
    }
}
```

## Troubleshooting

- **Enum Control Not Displaying Properly**: Ensure that the `EnumControl` component is attached to a GameObject with a `Dropdown` UI element.
- **Null Reference Errors**: Verify that the enum type is properly passed into the `SetupEnum` method.

## Future Improvements

- Add support for custom labels or values for each enum entry.
- Improve error handling and validation for unsupported enum types.
- Extend functionality to handle more UI controls (e.g., toggles or sliders) for enums with ranges.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
