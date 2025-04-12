@echo off

.\artifacts\TestFileGenerator.exe 100MB
.\artifacts\Sorter.exe

echo.
echo Check the home directory for output:
echo %USERPROFILE%
echo.
echo Input file: test.txt
echo Output file: sorted.txt
