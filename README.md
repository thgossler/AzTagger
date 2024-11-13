# AzTagger

AzTagger is a .NET 8 Windows Desktop GUI application built using WinForms. It is a helper tool that allows high-performance search and filtering of all Azure resources, resource groups, and subscriptions using Azure Resource Graph across all tenants and subscriptions that the signed-in user has permissions to access.

## Features

### User Authentication and Tenant Selection
- Use MSAL (Microsoft Authentication Library) for sign-in with Entra ID via web browser, including tenant selection and support for multi-factor authentication.
- Select the Azure Environment (e.g., `AzureCloud`, `AzureChinaCloud`) before querying for accessible tenants.
- Select from a list of all Entra ID tenants the user has access to in a drop-down list.
- Apply subsequent searches only to the selected tenant.
- Remember the selected tenant and Azure Environment upon exiting the application and reload them on the next start.

### Search Functionality
- Perform high-performance search and filtering of all Azure resources, resource groups, and subscriptions based on in-memory result data from Azure Resource Graph.
- Provide a single text input field for easy and flexible filtering of resources.

### Search Results Display
- Support large number of search results without slowing down interactions like scrolling.
- Enable column sorting and provide separate column-specific filter input fields above each column for quick filtering within that column's values.
- Double-clicking an item opens the Azure Portal GUI for that item in the system's default web browser.

### Tag Management
- Enable easy deletion of tags and inline editing of any item's key and value.
- Allow adding new tags by clicking into the last empty line's key or value cells and start typing.
- Store tag templates in a `tagtemplates.json` file in the user's AppData Local folder.
- Apply button to set or update all the specified tags on the selected subscriptions, resource groups, and resources.

### Application Settings and Environment
- Store user settings in a `settings.json` file in the user's AppData Local folder.

### Error Handling and Logging
- Log all errors via Serilog to an `errorlog.txt` file in the user's AppData Local folder.
- Provide user-friendly error messages in message boxes when errors occur.

### Performance and Responsiveness
- Ensure the application's responsiveness at all times by performing searches and tag updates on background threads.
- Using asynchronous programming patterns (`async`/`await`) for I/O-bound operations.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please read the [CONTRIBUTING](CONTRIBUTING.md) file for guidelines on how to contribute to this project.

## Sponsoring

If you find this project useful and would like to support its development, please consider sponsoring the project.

## Contact

For any questions or feedback, please open an issue on GitHub or contact the project maintainer.
