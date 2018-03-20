FROM opensuse:42.3

# Install the base toolchain we need to build anything (clang, cmake, make and the like)
# this does not include libraries that we need to compile different projects, we'd like
# them in a different layer.
RUN zypper -n install \
        cmake \
        hostname \
        lldb-devel \
        llvm-clang \
        llvm-devel \
        gcc-c++ \
        python-xml \
        which \
        wget \
    && ln -s /usr/bin/clang++ /usr/bin/clang++-3.5 \
    && zypper clean -a

# Install tools used by the VSO build automation.
RUN zypper -n install \
        git \
        npm \
        nodejs \
        tar \
        zip \
    && zypper clean -a

RUN npm install -g azure-cli

# Dependencies of CoreCLR and CoreFX.
RUN zypper -n install --force-resolution \
        glibc-locale \
        krb5-devel \
        libcurl-devel \
        libgdiplus0 \
        libicu-devel \
        libunwind-devel \
        libuuid-devel \
        lttng-ust-devel \
        libopenssl-devel \
        uuid-devel \
    && zypper clean -a

# Until OpenSuse.42.3 official packages are available, we have to restore the ubuntu ones instead.
ENV __PUBLISH_RID=ubuntu.14.04-x64

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g wheel
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permissions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chmod -R 755 /usr/lib/sudo

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code
