# Original example (ContractOps) uses .NET base because it includes a .NET project
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
# I don't think we need .NET. However, the base debian image doesn't have all the libraries
# pandoc wants, so we're using that for now
#FROM debian:12-slim

# Or perhaps we should just use https://hub.docker.com/r/pandoc/latex

# Install pandoc, MiKTeX, and rsvg-convert
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        dirmngr \
        gpg \
        gpg-agent \
        librsvg2-bin \
        wget \
        xz-utils \
    && apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys D6BC243565B2087BC3F897C9277A7293F59E4889 \
    && echo "deb http://miktex.org/download/debian bullseye universe" | tee /etc/apt/sources.list.d/miktex.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
        miktex \
    && miktexsetup finish \
    && wget https://github.com/jgm/pandoc/releases/download/3.1.6.2/pandoc-3.1.6.2-1-amd64.deb \
    && dpkg -i ./pandoc-3.1.6.2-1-amd64.deb \
    && rm ./pandoc-3.1.6.2-1-amd64.deb \
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /pandoc-lua-filters
RUN wget https://raw.githubusercontent.com/pandoc-ext/pagebreak/main/pagebreak.lua \
    && wget https://raw.githubusercontent.com/alpianon/pandoc-inline-headers/master/crossref-ordered-list \
    && mv crossref-ordered-list crossref-ordered-list.lua \
    && wget https://gitlab.com/howdyadoc/toolchain/-/raw/master/howdyadoc/lua-filters/inline-headers.lua

WORKDIR /pandoc-filters
RUN wget https://github.com/lierdakil/pandoc-crossref/releases/download/v0.3.16.0f/pandoc-crossref-Linux.tar.xz \
    && tar -xJf pandoc-crossref-Linux.tar.xz \
    && rm pandoc-crossref-Linux.tar.xz

# Allow MikTeX to install things it needs
RUN initexmf --set-config-value=[MPM]AutoInstall=yes

WORKDIR /app

RUN mkdir content
COPY ["build/pandoc.sh","pandoc.sh"]
COPY ["content","content"]
COPY ["templates","templates"]
COPY ["metadata.md","metadata.md"]
COPY ["intro-rx-dotnet-book-cover.png","intro-rx-dotnet-book-cover.png"]

VOLUME ["/output"]
ENTRYPOINT ["/bin/sh", "/app/pandoc.sh"]