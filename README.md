# Exclude From Build
## extension for Visual Studio 2013, 2015, 2017 and 2019

This extension allows to exclude from build or include in build multiple C/C++/C# files and directories with one click of a menu option.

1. Select directories and/or files you'd like to exclude or include
2. Go to menu **Tools**
3. Click either **Exclude from Build** or **Include in Build**

![Exclude From Build](images/preview.png)

Files are excluded/included based on file extension:
- **.c**, **.cc**, **.cpp**, **.cxx**
  - **Exclude** sets **Excluded from Build** file property to **Yes**/**True**
  - **Include** sets **Excluded from Build** file property to **No**/**False**
- **.cs**
  - **Exclude** sets **Build Action** file property to **None**
  - **Include** sets **Build Action** file property to **Compile**
- **.xaml**
  - **Exclude** sets **Build Action** file property to **None**
  - **Include** sets **Build Action** file property to **Page**

Note that **Page** property may be not correct for all XAML files, e.g. the file containing the Application definition requires **Build Action** set to **ApplicationDefinition**. So use with care.

For C/C++ it is possible to affect only the Active project configuration (default) or All project configurations. Choose one of the options in menu **Tools -> More**.
The default behavior of the main menu buttons can be changed in **Tools -> Options -> Exclude from Build**.

You can download this extension from [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=AdamWulkiewicz.ExcludeFromBuild) or [GitHub](https://github.com/awulkiew/exclude-from-build/releases).