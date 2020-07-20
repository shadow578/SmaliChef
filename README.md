# SmaliChef
Flavor your smali with "ease"

## Usage
```
$ smalichef.exe -?
Commandline:
-input    / -i=<DIR>
    input directory
-output   / -o=<DIR>
    output (mirror) directory
    contents will be overwritten

-flavor   / -f=<NAME>
    flavors to apply
    multiple flavors are allowed
-filter    / -ff=<FILTER>
    filter for files to process
    files not matching the filter will only be copied
    multiple filters are allowed
-marked   / -m
    only process files that are marked
    speeds up processing
-all      / -a
    always copy all files, even if unchanged
-parallel / -p
    process files in parallel

-debug    / -d
    enable debug logs
-verbose  / -v
    enable verbose logging
-vverbose / -vv
    enable VERY verbose logging
ex:
$ SmaliChef -i=./src -o=./src-flavored -m -p -f=themeblack -ff=*.smali -ff=*.xml


Expressions:
#[flavor]name="content";name="content";name="content";#[/flavor]
ex: 
<TextView name="#[flavor]foo="bar";test="string";#[/flavor]"/>
    with "-f=foo" results in
<TextView name="bar"/>

when running with -marked, mark flavored files by adding "flavored" to the first line in the file
ex:
<?xml version="1.0"?><!-- flavored -->
    or
.class Lfoo;#flavored
```