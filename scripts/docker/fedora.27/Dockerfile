FROM fedora:27

# Install packages required for core-setup build steps
RUN dnf install -y bash git cmake wget which python clang llvm-devel make libicu-devel lldb-devel.x86_64 \
                   libunwind-devel lttng-ust-devel uuid-devel libuuid-devel glibc-locale-source zlib-devel libcurl-devel \
                   krb5-devel openssl-devel autoconf libtool hostname compat-openssl10-devel

RUN dnf upgrade -y nss

RUN dnf clean all

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
