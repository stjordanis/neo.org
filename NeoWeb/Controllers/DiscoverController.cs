﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NeoWeb.Data;
using NeoWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace NeoWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DiscoverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _userId;
        private readonly bool _userRules;
        private readonly IHostingEnvironment _env;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public DiscoverController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IStringLocalizer<SharedResource> sharedLocalizer, IHostingEnvironment env)
        {
            _context = context;
            _sharedLocalizer = sharedLocalizer;
            _userId = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _env = env;
            if (_userId != null)
            {
                _userRules = _context.UserRoles.Any(p => p.UserId == _userId);
            }
        }

        // GET: discover
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(int? type = null, int? year = null, string keywords = null)
        {
            IQueryable<Blog> blogs = _context.Blogs;
            IQueryable<Event> events = _context.Events;
            IQueryable<News> news = _context.News;

            List<DiscoverViewModel> viewModels = new List<DiscoverViewModel>();

            // year filter
            if (year != null)
            {
                blogs = blogs.Where(p => p.CreateTime.Year == year);
                events = events.Where(p => p.StartTime.Year == year);
                news = news.Where(p => p.Time.Year == year);
            }

            // keywords filter
            if (!string.IsNullOrEmpty(keywords))
            {
                foreach (var item in keywords.Split(" "))
                {
                    blogs = blogs.Where(p => p.ChineseTitle.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.ChineseContent.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.ChineseTags != null && p.ChineseTags.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.ChineseSummary != null && p.ChineseSummary.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.EnglishTitle.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.EnglishContent.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.EnglishTags != null && p.EnglishTags.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.EnglishSummary != null && p.EnglishSummary.Contains(item, StringComparison.OrdinalIgnoreCase));
                    if (blogs == null) break;
                }
                foreach (var item in keywords.Split(" "))
                {
                    switch (item.ToLower())
                    {
                        case "conference": events = events.Where(p => p.Type == EventType.Conference); break;
                        case "meetup": events = events.Where(p => p.Type == EventType.Meetup); break;
                        case "workshop": events = events.Where(p => p.Type == EventType.Workshop); break;
                        case "hackathon": events = events.Where(p => p.Type == EventType.Hackathon); break;
                        default:
                            events = events.Where(p => p.ChineseAddress.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.ChineseCity.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.ChineseDetails.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.ChineseName.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.ChineseOrganizers.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.Country != null && p.Country.ZhName.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.Country != null && p.Country.Name.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.EnglishAddress.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.EnglishCity.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.EnglishDetails.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.EnglishName.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.EnglishOrganizers.Contains(item, StringComparison.OrdinalIgnoreCase)
                                || p.ThirdPartyLink != null && p.ThirdPartyLink.Contains(item, StringComparison.OrdinalIgnoreCase)); break;
                    }
                    if (events == null) break;
                }
                foreach (var item in keywords.Split(" "))
                {
                    news = news.Where(p => p.ChineseTitle.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.EnglishTitle.Contains(item, StringComparison.OrdinalIgnoreCase)
                        || p.Link.Contains(item, StringComparison.OrdinalIgnoreCase));
                    if (news == null) break;
                }
            }

            // 中英文切换
            // type filter
            bool isZh = _sharedLocalizer["en"] == "zh";
            switch (type)
            {
                case (int)DiscoverViewModelType.Blog:
                    AddBlogs(blogs, viewModels, isZh);
                    break;
                case (int)DiscoverViewModelType.Event:
                    AddEvents(events, viewModels, isZh);
                    break;
                case (int)DiscoverViewModelType.News:
                    AddNews(news, viewModels, isZh);
                    break;
                default:
                    AddBlogs(blogs, viewModels, isZh);
                    AddEvents(events, viewModels, isZh);
                    AddNews(news, viewModels, isZh);
                    break;
            }

            viewModels = viewModels.OrderByDescending(p => p.Time).ToList();

            // 添加置顶内容
            DiscoverViewModel onTop = viewModels.FirstOrDefault();
            if (onTop != null)
            {
                switch (onTop.Type)
                {
                    case DiscoverViewModelType.Blog:
                        Blog blg = _context.Blogs.Single(p => p.Id == onTop.Blog.Id);
                        if (isZh)
                            onTop.Blog.Summary = blg.ChineseSummary;
                        else
                            onTop.Blog.Summary = blg.EnglishSummary;
                        break;
                    case DiscoverViewModelType.Event:
                        Event evt = _context.Events.Single(p => p.Id == onTop.Event.Id);
                        if (isZh)
                            onTop.Event.Details = evt.ChineseDetails;
                        else
                            onTop.Event.Details = evt.EnglishDetails;
                        break;
                    case DiscoverViewModelType.News:
                        break;
                    default:
                        break;
                } 
            }

            ViewBag.OnTop = onTop;
            ViewBag.UserRules = _userRules;

            return View(viewModels);
        }

        private void AddBlogs(IQueryable<Blog> blogs, List<DiscoverViewModel> viewModels, bool isZh)
        {
            List<BlogViewModel> blogList;
            if (isZh)
            {
                blogList = blogs.Select(p => new BlogViewModel()
                {
                    Id = p.Id,
                    CreateTime = p.CreateTime,
                    Title = p.ChineseTitle,
                    Tags = p.ChineseTags,
                    Cover = p.ChineseCover
                }).ToList();
            }
            else
            {
                blogList = blogs.Select(p => new BlogViewModel()
                {
                    Id = p.Id,
                    CreateTime = p.CreateTime,
                    Title = p.EnglishTitle,
                    Tags = p.EnglishTags,
                    Cover = p.EnglishCover
                }).ToList();
            }
            foreach (var item in blogList)
                viewModels.Add(new DiscoverViewModel()
                {
                    Type = DiscoverViewModelType.Blog,
                    Blog = item,
                    Time = item.CreateTime
                });
        }

        private void AddEvents(IQueryable<Event> events, List<DiscoverViewModel> viewModels, bool isZh)
        {
            List<EventViewModel> eventList;
            if (isZh)
            {
                eventList = events.Select(p => new EventViewModel()
                {
                    Id = p.Id,
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    Name = p.ChineseName,
                    Country = p.Country.ZhName,
                    City = p.ChineseCity,
                    Cover = p.ChineseCover
                }).ToList();
            }
            else
            {
                eventList = events.Select(p => new EventViewModel()
                {
                    Id = p.Id,
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    Name = p.EnglishName,
                    Country = p.Country.Name,
                    City = p.EnglishCity,
                    Cover = p.EnglishCover
                }).ToList();
            }
            foreach (var item in eventList)
                viewModels.Add(new DiscoverViewModel()
                {
                    Type = DiscoverViewModelType.Event,
                    Event = item,
                    Time = item.StartTime
                });
        }

        private void AddNews(IQueryable<News> news, List<DiscoverViewModel> viewModels, bool isZh)
        {
            List<NewsViewModel> newsList;
            if (isZh)
            {
                newsList = news.Select(p => new NewsViewModel()
                {
                    Id = p.Id,
                    Title = p.ChineseTitle,
                    Time = p.Time,
                    Link = p.Link,
                    Cover = p.ChineseCover
                }).ToList();
            }
            else
            {
                newsList = news.Select(p => new NewsViewModel()
                {
                    Id = p.Id,
                    Title = p.EnglishTitle,
                    Time = p.Time,
                    Link = p.Link,
                    Cover = p.EnglishCover
                }).ToList();
            }
            foreach (var item in newsList)
                viewModels.Add(new DiscoverViewModel()
                {
                    Type = DiscoverViewModelType.News,
                    News = item,
                    Time = item.Time
                });
        }
    }
}