FROM fedora:27

# Workaround to avoid NuGet restore timeout
RUN dnf upgrade -y nss \
    && dnf clean all

# Install the base toolchain we need to build anything (clang, cmake, make and the like)
# this does not include libraries that we need to compile different projects, we'd like
# them in a different layer.
RUN dnf install -y \
        clang \
        cmake \
        lldb-devel \
        llvm-devel \
        make \
        python \
        which \
    && dnf clean all

# Install tools used by the VSO build automation.
RUN dnf install -y \
        git \
        npm \
        nodejs \
        tar \
        zip \
    && dnf clean all

RUN npm install -g azure-cli --unsafe-perm

# Dependencies of CoreCLR and CoreFX.
RUN dnf install -y \
        glibc-locale-source \
        libcurl-devel \
        libgdiplus \
        libicu-devel \
        libunwind-devel \
        libuuid-devel \
        lttng-ust-devel \
        openssl-devel \
        uuid-devel \
    && dnf clean all

RUN dnf install -y compat-openssl10-devel --allowerasing

# Until Fedora.27-x64 official packages are available, we have to restore the ubuntu ones instead.
ENV __PUBLISH_RID=ubuntu.14.04-x64

# Setup User to match Host User, and give superuser permissions 
ARG USER_ID=0 
RUN useradd -m code_executor -u ${USER_ID} -g wheel
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers 
 
# With the User Change, we need to change permissions on these directories 
RUN chmod -R a+rwx /usr/local 
RUN chmod -R a+rwx /home

# Force buildtools to publish for Fedora 23
ENV __PUBLISH_RID=fedora.23-x64
 
# Set user to the one we just created 
USER ${USER_ID} 
 
# Set working directory 
WORKDIR /opt/code
