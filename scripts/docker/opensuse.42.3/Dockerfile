FROM microsoft/dotnet-buildtools-prereqs:opensuse-42.3-d46ee12-20180327014902

# Install packages required for core-setup build steps

RUN zypper -n install \
        curl \
        libstdc++-devel \
        ncurses-utils \
    && zypper clean -a

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
