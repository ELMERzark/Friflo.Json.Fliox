
dotnet build -c Release --verbosity:quiet

rem dotnet run -c Release --no-build --framework net6.0 --module DropDatabase

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=file

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=sqlite

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=postgres

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=sqlserver
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=sqlserver_rel

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mysql
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mysql_rel

dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mariadb
dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=mariadb_rel

rem dotnet test -c Release --consoleloggerparameters:ErrorsOnly --filter TestCategory=test_db --no-build -e TEST_DB_PROVIDER=cosmos

pause