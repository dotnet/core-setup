FROM microsoft/dotnet-buildtools-prereqs:fedora-27-82a3800-20180326211504

# Workaround to avoid NuGet restore timeout
RUN dnf upgrade -y nss \
    && dnf clean all

# Install packages required for core-setup build steps
RUN dnf install -y \
        wget \
        curl \
        autoconf \
        libtool \
        hostname \
    && dnf clean all

# Setup User to match Host User, and give superuser permissions 
ARG USER_ID=0 
RUN useradd -m code_executor -u ${USER_ID} -g wheel
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers 
 
# With the User Change, we need to change permissions on these directories 
RUN chmod -R a+rwx /usr/local 
RUN chmod -R a+rwx /home
 
# Set user to the one we just created 
USER ${USER_ID} 
 
# Set working directory 
WORKDIR /opt/code
