FROM microsoft/dotnet-buildtools-prereqs:debian-stretch-bd8184b-20180410224356

# Misc Dependencies for build
RUN apt-get update && \
    apt-get -qqy install \
        wget \
        unzip \
        gettext \
        libuuid1 \
        sudo  \
        debhelper \
        build-essential \
        devscripts && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Use clang as c++ compiler
RUN update-alternatives --install /usr/bin/c++ c++ /usr/bin/clang++ 100
RUN update-alternatives --set c++ /usr/bin/clang++

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g sudo
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permissions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chmod -R 755 /usr/lib/sudo

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code
