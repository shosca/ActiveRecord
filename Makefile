BASEDIR=$(shell pwd)
PROJ=Castle.ActiveRecord
BUILDDIR=build
OUTPUTPATH=$(BASEDIR)/$(BUILDDIR)
SLN=$(PROJ).sln
VERSION=4.0.0.0

CONFIG=release
FW=v4.0
FILES:=$(shell find src -name '*.cs' -print)

ifdef WINDIR
MSBUILD=MSBuild.exe
else
MSBUILD=xbuild
endif
-include config.mk

all: test

files:
	echo $(FILES)

$(OUTPUTPATH)/$(CONFIG)/$(FW)/$(PROJ).dll: $(FILES) $(BASEDIR)/CommonAssemblyInfo.cs
	$(MSBUILD) $(SLN) /p:Configuration=$(CONFIG) /p:OutputPath=$(OUTPUTPATH)/$(CONFIG)/$(FW) /p:TargetFrameworkVersion=$(FW)

$(BASEDIR)/CommonAssemblyInfo.cs:
	@echo "using System.Reflection; \
	[assembly: AssemblyCompany(\"Castle Project\")] \
	[assembly: AssemblyCopyright(\"Copyright (c) 2004-2010 Castle Project - http://www.castleproject.org\")] \
	[assembly: AssemblyVersion(\"$(VERSION)\")] \
	[assembly: AssemblyFileVersion(\"$(VERSION)\")] \
	[assembly: AssemblyInformationalVersion(\"$(shell git log -1 --format="%H")\")]" > $(BASEDIR)/CommonAssemblyInfo.cs

build: $(OUTPUTPATH)/$(CONFIG)/$(FW)/$(PROJ).dll


clean:
	$(MSBUILD) $(SLN) /p:OutputPath=$(OUTPUTPATH)/$(CONFIG)/$(FW) /t:Clean
	rm -rf $(OUTPUTPATH)
	find $(BASEDIR)/src -type f -iname CommonAssemblyInfo.cs -prune -exec rm -f {} \;
	find $(BASEDIR)/src -type d -iname bin -prune -exec rm -rf {} \;
	find $(BASEDIR)/src -type d -iname build -prune -exec rm -rf {} \;
	find $(BASEDIR)/src -type d -iname obj -prune -exec rm -rf {} \;

test: build
	$(shell find packages -name nunit-console.exe) $(shell find $(BUILDDIR) -name $(PROJ).Tests.dll)

zip: clean release build
	tar cva --transform 's,^./,$(PROJ)-$(VERSION)/,S' \
		--exclude '.git*' \
		--exclude 'obj' \
		--exclude '*.suo' \
		--exclude '*.pdb' \
		--exclude '*.user' \
		--exclude '*.userprefs' \
		--exclude '_ReSharper*' \
		--exclude 'Thumbs.db' \
		-f ../$(PROJ)-$(VERSION).tar.gz .
