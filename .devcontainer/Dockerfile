ARG USERNAME=app_user
ARG GROUPNAME=app_user

FROM    registry.access.redhat.com/ubi9/ubi-init:9.4 as common_build
ARG USERNAME
ARG GROUPNAME

# RUN --mount=type=secret,id=host_certificate_file,dst=/etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem \
#     dnf clean all && dnf update -y
RUN --mount=type=secret,id=host_certificate_file,dst=/etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem \
    dnf install -y \
    langpacks-en \
    wget \
    unzip \
    httpd \
    gcc \
    dotnet-sdk-9.0 \
    rust-toolset \
&&  groupadd $GROUPNAME \
&&  useradd -m -s /bin/bash -g $GROUPNAME $USERNAME \
&&  dnf config-manager --add-repo=https://download.docker.com/linux/centos/docker-ce.repo \
&&  dnf install -y \
    sudo \
    git \
    vim \
    diffutils \
    bind-utils \
    iputils \
    docker-ce-cli \
    jq \
&&  npm config set registry http://registry.npmjs.org/ \
&&  usermod -G wheel $USERNAME \
&&  echo '%wheel ALL=(ALL) NOPASSWD: ALL' >> /etc/sudoers \
&&  echo "%${GROUPNAME} ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers \
&&  cargo install cargo-expand@1.0.111 --locked


USER app_user