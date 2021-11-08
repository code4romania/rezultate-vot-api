# Rezultate Vot API

[![Docker tag](https://img.shields.io/docker/v/code4romania/rezultate-vot-api?style=for-the-badge)](https://hub.docker.com/r/code4romania/rezultate-vot-api/tags)
[![GitHub contributors](https://img.shields.io/github/contributors/code4romania/rezultate-vot-api.svg?style=for-the-badge)](https://github.com/code4romania/rezultate-vot-api/graphs/contributors) [![GitHub last commit](https://img.shields.io/github/last-commit/code4romania/rezultate-vot-api.svg?style=for-the-badge)](https://github.com/code4romania/rezultate-vot-api/commits/master) [![License: MPL 2.0](https://img.shields.io/badge/license-MPL%202.0-brightgreen.svg?style=for-the-badge)](https://opensource.org/licenses/MPL-2.0)

O democrație se sprijină pe cetățeni critici și informați. Rezultate Vot își propune să informeze și să dezvolte spiritul critic al alegătorilor prin contextualizarea informației electorale însoțite de analize apartinice ale acesteia. Această platformă este locul în care oricine poate accesa toate informațiile relevante ale alegerilor din România.

[See the project live](https://rezultatevot.ro)

Pe Rezultate Vot veți găsi:

- Hărți detaliate pe care puteți vizualiza prezența la vot la nivelul țării / la nivel de județ;
- Rezultatele parțiale ale alegerilor, după închiderea urnelor, pe măsură ce ele sunt comunicate de autorități;
- Informații din sistemul de monitorizare digitală a alegerilor - Monitorizare Vot - realizat de Code for Romania și utilizat de observatorii alegerilor
- Istoricul electoral al României pentru toate rundele de alegeri începând cu anul 1992
- Un flux live de comentarii și analize realizate de sociologi din marile centre universitare din România.

[Contributing](#contributing) | [Built with](#built-with) | [Repos and projects](#repos-and-projects) | [Deployment](#deployment) | [Feedback](#feedback) | [License](#license) | [About Code4Ro](#about-code4ro)

## Contributing

This project is built by amazing volunteers and you can be one of them! Here's a list of ways in [which you can contribute to this project](https://github.com/code4romania/.github/blob/master/CONTRIBUTING.md). If you want to make any change to this repository, please **make a fork first**.

If you would like to suggest new functionality, open an Issue and mark it as a **[Feature request]**. Please be specific about why you think this functionality will be of use. If you can, please include some visual description of what you would like the UI to look like, if you are suggesting new UI elements.

## Built With

.net core 3.1

### Programming languages

C# 8

## Repos and projects

Client App - https://github.com/code4romania/rezultate-vot-client

## Deployment

### Stage

Every push to develop branch should trigger a new deploy on stage server

### Production

1. Add a new tag to the repository
2. Go to the [Code4ro k8s manifest repo](https://github.com/code4romania/code4ro-k8s) and update the k8s manifest with the new image tag

## Feedback

- Request a new feature on GitHub.
- Vote for popular feature requests.
- File a bug in GitHub Issues.
- Email us with other feedback contact@code4.ro

## License

This project is licensed under the MPL 2.0 License - see the [LICENSE](LICENSE) file for details

## About Code4Ro

Started in 2016, Code for Romania is a civic tech NGO, official member of the Code for All network. We have a community of over 500 volunteers (developers, ux/ui, communications, data scientists, graphic designers, devops, it security and more) who work pro-bono for developing digital solutions to solve social problems. #techforsocialgood. If you want to learn more details about our projects [visit our site](https://www.code4.ro/en/) or if you want to talk to one of our staff members, please e-mail us at contact@code4.ro.

Last, but not least, we rely on donations to ensure the infrastructure, logistics and management of our community that is widely spread across 11 timezones, coding for social change to make Romania and the world a better place. If you want to support us, [you can do it here](https://code4.ro/en/donate/).
