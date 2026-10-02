This application features a simple tree view application built using .net 10.0 and C# 14.0.
It allows users to create, edit, and delete tree nodes, supports manual loading of tree nodes and saves all the changes in cache before synchronizing with the database.

For Database, it uses SQLServer. You need to provide your own connection string in the appsettings.Development.json file.
The application ensures that Database is created and properly seeded with initial data on the first run.

In terms of UI, it uses one Razor page to display the tree view and a modal dialog for creating and editing nodes.
The Razor page is simple but effective, providing a clear view of the tree structure and easy access to node management features.