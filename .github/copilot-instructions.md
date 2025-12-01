# Code Style
## Documentation

00. All C# project should use the latest version of C# Syntax and best practices.
10. For each class, it should have the summary of what it does and example usage, and for each method, it should have the summary of what it does, the parameters it takes, and what it returns.
20. Each class file should have a header comment that describes the file's purpose and any important details about its contents, author, date. 
	1. Author: Gary Wu
	2. Project: ### <Project Name>
	3. Date: Today's date.
30. Different purpose of documentation should be inside the related XML tag. For example, the code example should be in a code, and if any warning or note is needed, it should be inside the related tag.
	1. Wrap your code in a `CDATA` section inside `<code>`
40. Use consistent terminology throughout the codebase.