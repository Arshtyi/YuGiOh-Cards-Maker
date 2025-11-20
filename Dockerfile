FROM registry.fedoraproject.org/fedora:43
ENV DOTNET_ROOT=/usr/lib64/dotnet
ENV PATH=$PATH:/usr/lib64/dotnet
RUN dnf -y upgrade \
    && dnf -y install wget curl unzip jq ImageMagick python3 which tar xz
RUN rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm \
    && dnf -y install dotnet-sdk-8.0 \
    && dnf -y clean all \
    && rm -rf /var/cache/dnf
WORKDIR /app
COPY . /app
RUN chmod +x ./YuGiOh-Cards-Maker.sh || true
COPY ./entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh
RUN useradd -m yugioh || true
RUN chown -R yugioh:yugioh /app
USER yugioh
ENV LANG=C.UTF-8
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
