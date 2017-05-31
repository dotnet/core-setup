#!/usr/bin/env bash

usage()
{
    echo "Creates a toolchain and sysroot used for cross-compiling for Android."
    echo.
    echo "Usage: $0 [BuildArch] [ApiLevel]"
    echo.
    echo "BuildArch is the target architecture of Android."
    echo "ApiLevel is the target Android API level. API levels usually match to Android releases. See https://source.android.com/source/build-numbers.html"
    echo.
    echo "By default, the toolchain and sysroot will be generated in cross/android-rootfs/toolchain/[BuildArch]. You can change this behavior"
    echo "by setting the TOOLCHAIN_DIR environment variable"
    echo.
    echo "By default, the NDK will be downloaded into the cross/android-rootfs/android-ndk-r[versionname] directory. If you already have an NDK installation,"
    echo "you can set the NDK_DIR environment variable to have this script use that installation of the NDK."
    exit 1
}

#Defaults:  These are the default values that will be used if no arguments are passed to script.  Right now script doesn't support param NDK version
__ApiLevel=21 # The minimum platform for arm64 is API level 21
__BuildArch=arm64
__AndroidArch=aarch64
__AndroidToolchain=aarch64-linux-android
__NDK_Version=r14

#From corefx PR16819, adds ARM support
for i in "$@"
    do
        lowerI="$(echo $i | awk '{print tolower($0)}')"
        case $lowerI in
        -?|-h|--help)
            usage
            exit 1
            ;;
        arm64)
            __BuildArch=arm64
            __AndroidArch=aarch64
	    __AndroidToolchain=aarch64-linux-android
            # Old value...is this correct toolchain for arm64?
            #__AndroidToolchain=arm-linux-androideabi
            ;;
        arm)
            __BuildArch=arm
            __AndroidArch=arm
            __AndroidToolchain=arm-linux-androideabi
            ;;
        *[0-9])
            __ApiLevel=$i
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
            ;;
    esac
done

# Obtain the location of the bash script to figure out where the root of the repo is.
__CrossDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Set up directories based on root of repo
__Android_Cross_Dir="$__CrossDir/android-rootfs"
__NDK_Dir="$__Android_Cross_Dir/android-ndk-$__NDK_Version"
__libunwind_Dir="$__Android_Cross_Dir/libunwind"
__ToolchainDir="$__Android_Cross_Dir/toolchain/$__BuildArch"
__libunwind_cfxdir=__libunwind_clrdir=`sudo find / -wholename "*/coreclr/cross/android-rootfs/toolchain/arm64/sysroot/usr"`

# Check if environment variables are set
if [[ -n "$TOOLCHAIN_DIR" ]]; then
    __ToolchainDir=$TOOLCHAIN_DIR
fi

if [[ -n "$NDK_DIR" ]]; then
    __NDK_Dir=$NDK_DIR
fi

echo "Target API level: $__ApiLevel"
echo "Target architecture: $__BuildArch"
echo "NDK location: $__NDK_Dir"
echo "Target Toolchain location: $__ToolchainDir"
#TODO:  Echo result of check for existing cross-compiled libunwind files in corefx repo (line 65)

# Download the NDK if required
if [ ! -d $__NDK_Dir ]; then
    echo Downloading the NDK into $__NDK_Dir
    mkdir -p $__NDK_Dir
    wget -nv -nc --show-progress https://dl.google.com/android/repository/android-ndk-$__NDK_Version-linux-x86_64.zip -O $__Android_Cross_Dir/android-ndk-$__NDK_Version-linux-x86_64.zip
    unzip -q $__Android_Cross_Dir/android-ndk-$__NDK_Version-linux-x86_64.zip -d $__Android_Cross_Dir
fi

# Create the RootFS for both arm64 as well as aarch
rm -rf $__Android_Cross_Dir/toolchain

echo Generating the $__BuildArch toolchain
$__NDK_Dir/build/tools/make_standalone_toolchain.py --arch $__BuildArch --api $__ApiLevel --install-dir $__ToolchainDir

# Install the required packages into the toolchain
rm -rf $__Android_Cross_Dir/deb/
rm -rf $__Android_Cross_Dir/tmp

mkdir -p $__Android_Cross_Dir/deb/
mkdir -p $__Android_Cross_Dir/tmp/$arch/

# Getting CoreCLR dependencies
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libicu_58.2_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libicu_58.2_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libicu-dev_58.2_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libicu-dev_58.2_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libuuid-dev_1.0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libuuid-dev_1.0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libuuid_1.0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libuuid_1.0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-glob-dev_0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-glob-dev_0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-glob_0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-glob_0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-support-dev_13.10_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-support-dev_13.10_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-support_13.10_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-support_13.10_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/liblzma-dev_5.2.3_$__AndroidArch.deb  -O $__Android_Cross_Dir/deb/liblzma-dev_5.2.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/liblzma_5.2.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/liblzma_5.2.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libcurl-dev_7.52.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libcurl-dev_7.52.1_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libcurl_7.52.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libcurl_7.52.1_$__AndroidArch.deb

# getting CoreFX dependencies
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/krb5-dev_1.15_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/krb5-dev_1.15_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/krb5_1.15_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/krb5_1.15_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/openssl-dev_1.0.2k_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/openssl-dev_1.0.2k_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/openssl_1.0.2k_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/openssl_1.0.2k_$__AndroidArch.deb


echo Unpacking Termux packages
# unpacking CoreCLR dependencies
dpkg -x $__Android_Cross_Dir/deb/libicu_58.2_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libicu-dev_58.2_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libuuid-dev_1.0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libuuid_1.0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-glob-dev_0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-glob_0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-support-dev_13.10_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-support_13.10_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/liblzma-dev_5.2.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/liblzma_5.2.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libcurl-dev_7.52.1_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libcurl_7.52.1_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/

#unpacking CoreFX dependencies
dpkg -x $__Android_Cross_Dir/deb/krb5-dev_1.15_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/krb5_1.15_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/openssl-dev_1.0.2k_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/openssl_1.0.2k_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/

cp -R $__Android_Cross_Dir/tmp/$__AndroidArch/data/data/com.termux/files/usr/* $__ToolchainDir/sysroot/usr/

# ifaddrs.h is not available natively Android, but a version of it is available
# See https://github.com/termux/termux-packages/issues/338 for context
wget -nv -nc https://raw.githubusercontent.com/qnnnnez/android-ifaddrs/master/include/ifaddrs_single.h -O $__ToolchainDir/sysroot/usr/include/ifaddrs.h

# Prepare libunwind

#TODO: Fix conditionals so that libunwind not builtif files exist
# Should do 'if corefx libunwind header files and corefx libunwind lib files exist, then copy them to correct location'
if [[ -d $__libunwind_cfxdir/lib/libunwind* && -d $__libunwind_cfxdir/include/libunwind* ]]; then
# if [ ! -d $__ToolchainDir/sysroot/usr/lib/libunwind* ]; then
# if [ ! -d $__ToolchainDir/sysroot/usr/include/libunwind* ]; then

    cp -r $__libunwind_cfxdir/lib/libunwind* $__ToolchainDir/sysroot/usr/lib/
    cp -r $__libunwind_cfxdir/include/libunwind* $__ToolchainDir/sysroot/usr/include/

else # clone and build libunwind

# Currently, we clone a fork of libunwind which adds support for Android; once this fork has been
# merged back in, this script can be updated to use the official libunwind repository.
# There's also an Android fork of libunwind which is currently not used.
#   git clone https://android.googlesource.com/platform/external/libunwind/ $__libunwind_Dir
#   git clone https://github.com/libunwind/libunwind/ $__libunwind_Dir
   git clone https://github.com/qmfrederik/libunwind/ $__libunwind_Dir

cd $__libunwind_Dir
git checkout features/android
git checkout -- .
git clean -xfd

# libunwind is available on Android, but not included in the NDK.
echo Building libunwind
autoreconf --force -v --install 2> /dev/null
./configure CC=$__ToolchainDir/bin/$__AndroidArch-linux-android-clang --with-sysroot=$__ToolchainDir/sysroot --host=$__AndroidArch-eabi --target=$__AndroidArch-eabi --disable-tests --disable-coredump --prefix=$__ToolchainDir/sysroot/usr 2> /dev/null
make > /dev/null
make install > /dev/null

# This header file is missing
cp include/libunwind.h $__ToolchainDir/sysroot/usr/include/
fi
#fi

__RootfsDir="$__ToolchainDir/sysroot"
if [[ -n "$ROOTFS_DIR" ]]; then
	__RootfsDir=$ROOTFS_DIR
fi


echo Now run:
echo CONFIG_DIR=\`realpath cross/android/arm64\` ROOTFS_DIR=\`realpath $__ToolchainDir/sysroot\` ./build.sh cross arm64 skipgenerateversion skipnuget cmakeargs -DENABLE_LLDBPLUGIN=0

