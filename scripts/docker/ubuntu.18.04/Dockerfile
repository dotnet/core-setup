#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Dockerfile that creates a container suitable to build dotnet-core-setup
FROM microsoft/dotnet-buildtools-prereqs:ubuntu-18.04-debpkg-66216d3-20180320152209

# Misc Dependencies for build
RUN apt-get update && \
    apt-get -qqy install \
        curl \
        libcurl3 \
        unzip \
        gettext \
        sudo && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
    
RUN apt-get update && \
    apt-get -qqy install \
        libcurl3 \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g sudo
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permissions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code